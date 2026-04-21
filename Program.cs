using System.IO.Compression;
using System.Net;
using System.Threading.RateLimiting;
using CrmWebApi;
using CrmWebApi.Common;
using CrmWebApi.Exceptions;
using CrmWebApi.Extensions;
using CrmWebApi.Filters;
using FluentValidation;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.OpenApi;
using Prometheus;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Templates;
using Serilog.Templates.Themes;

var builder = WebApplication.CreateBuilder(args);

// Логирование через Serilog: формат вывода в консоль, глушим шумные категории ASP.NET и EF
builder.Host.UseSerilog(
	(context, config) =>
	{
		config
			.WriteTo.Console(
				new ExpressionTemplate(
					"[{@t:HH:mm:ss} {@l:u3}] {Coalesce(Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1), '<no source>')} → {@m}\n{@x}",
					theme: context.HostingEnvironment.IsProduction() ? null : TemplateTheme.Code
				)
			)
			.MinimumLevel.Information()
			.MinimumLevel.Override(
				"Microsoft.AspNetCore.Hosting",
				Serilog.Events.LogEventLevel.Warning
			)
			.MinimumLevel.Override(
				"Microsoft.AspNetCore.Routing",
				Serilog.Events.LogEventLevel.Warning
			)
			.MinimumLevel.Override(
				"Microsoft.AspNetCore.Diagnostics",
				Serilog.Events.LogEventLevel.Warning
			)
			.MinimumLevel.Override(
				"Microsoft.AspNetCore.Authorization",
				Serilog.Events.LogEventLevel.Warning
			)
			.MinimumLevel.Override(
				"Microsoft.AspNetCore.Authentication",
				Serilog.Events.LogEventLevel.Warning
			)
			.MinimumLevel.Override(
				"Microsoft.EntityFrameworkCore.Database.Command",
				Serilog.Events.LogEventLevel.Warning
			);

		// В dev-окружении дублируем логи в Debug-окно IDE
		if (!context.HostingEnvironment.IsProduction())
			config.WriteTo.Debug();
	}
);

// Гибридный кеш (in-memory + distributed), TTL 1 минута
builder.Services.AddHybridCache(opt =>
{
	opt.DefaultEntryOptions = new() { Expiration = TimeSpan.FromMinutes(1) };
});

// Контроллеры + валидация через фильтр + AOT-совместимая JSON-сериализация
builder
	.Services.AddControllers(opt => opt.Filters.Add<ValidationFilter>())
	.AddJsonOptions(opt =>
		opt.JsonSerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonContext.Default)
	);

builder.Services.Configure<ApiBehaviorOptions>(opt =>
{
	opt.InvalidModelStateResponseFactory = context =>
		ApiProblemDetails.ToActionResult(
			ApiProblemDetails.FromModelState(context.ModelState, context.HttpContext)
		);
});

// FluentValidation: русский язык по умолчанию + авто-регистрация валидаторов
ValidatorOptions.Global.LanguageManager.Culture = new System.Globalization.CultureInfo("ru");
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Регистрация бизнес-сервисов (extension method)
builder.Services.AddServices();

// OpenAPI-документация с JWT Bearer схемой авторизации
builder.Services.AddOpenApi(opt =>
{
	opt.AddDocumentTransformer(
		(document, _, _) =>
		{
			var components = document.Components ?? new();
			components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
			components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
			{
				Type = SecuritySchemeType.Http,
				Scheme = "bearer",
				BearerFormat = "JWT",
				Description = "Enter your JWT token",
			};
			document.Components = components;

			document.Security ??= [];
			document.Security.Add(
				new OpenApiSecurityRequirement
				{
					[new OpenApiSecuritySchemeReference("Bearer")] = [],
				}
			);
			return Task.CompletedTask;
		}
	);
});

// PostgreSQL + EF Core
builder.Services.AddDatabase(builder.Configuration);

// Health checks: БД + SMTP
builder
	.Services.AddHealthChecks()
	.AddNpgSql(builder.Configuration.GetConnectionString("Default")!)
	.AddCheck<CrmWebApi.Health.SmtpHealthCheck>("smtp", tags: ["mail"]);

// Репозитории
builder.Services.AddRepositories();

// JWT-аутентификация
builder.Services.AddJwt(builder.Configuration);

// Авторизация (политики)
builder.Services.AddAuthorization();

// RFC 7807 формат ошибок
builder.Services.AddProblemDetails();

// Обработка ошибок БД (unique violation и т.д.)
builder.Services.AddExceptionHandler<DbExceptionHandler>();

// Глобальный обработчик исключений
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();


var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

// CORS: same-origin в проде, localhost для локальной разработки
builder.Services.AddCors(options =>
{
	options.AddPolicy(
		"AllowFrontend",
		policy =>
			policy
				.WithOrigins(corsOrigins)
				.AllowAnyHeader()
				.AllowAnyMethod()
				.AllowCredentials()
	);
});

// Rate limiting
builder.Services.AddRateLimiter(options =>
{
	// Глобальный лимит: 100 запросов/мин на IP (скользящее окно, 6 сегментов по 10 сек)
	options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
		RateLimitPartition.GetSlidingWindowLimiter(
			context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
			_ => new SlidingWindowRateLimiterOptions
			{
				PermitLimit = 100,
				Window = TimeSpan.FromMinutes(1),
				SegmentsPerWindow = 6,
				QueueLimit = 0,
			}
		)
	);

	// Строгий лимит для auth-эндпоинтов: 10 запросов/мин на IP
	options.AddPolicy(
		"auth",
		context =>
			RateLimitPartition.GetFixedWindowLimiter(
				context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
				_ => new FixedWindowRateLimiterOptions
				{
					PermitLimit = 10,
					Window = TimeSpan.FromMinutes(1),
					QueueLimit = 0,
				}
			)
	);
	options.OnRejected = async (context, ct) =>
	{
		Dictionary<string, object?>? extensions = null;
		if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
			extensions = new Dictionary<string, object?>
			{
				["retryAfterSeconds"] = Math.Ceiling(retryAfter.TotalSeconds),
			};

		await ApiProblemDetails.WriteAsync(
			context.HttpContext,
			ApiProblemDetails.FromStatus(
				StatusCodes.Status429TooManyRequests,
				"Слишком много запросов",
				context.HttpContext,
				extensions
			),
			ct
		);
	};
});

// Сжатие ответов: Brotli + Gzip, приоритет — скорость
builder.Services.AddResponseCompression(opt =>
{
	opt.EnableForHttps = true;
	opt.Providers.Add<BrotliCompressionProvider>();
	opt.Providers.Add<GzipCompressionProvider>();
});

builder.Services.Configure<BrotliCompressionProviderOptions>(opt =>
	opt.Level = CompressionLevel.Fastest
);
builder.Services.Configure<GzipCompressionProviderOptions>(opt =>
	opt.Level = CompressionLevel.Fastest
);

var app = builder.Build();

// Читаем реальный IP клиента из X-Forwarded-For (Caddy проставляет)
var forwardedOptions = new ForwardedHeadersOptions
{
	ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
	ForwardLimit = 1
};

forwardedOptions.KnownProxies.Add(IPAddress.Parse("127.0.0.1"));
app.UseForwardedHeaders(forwardedOptions);

// HTTPS-редирект и HSTS первыми — до любой обработки контента
if (app.Environment.IsProduction())
{
	app.UseHttpsRedirection();
	app.UseHsts();
}

// Security-заголовки
app.Use(
	async (ctx, next) =>
	{
		ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
		ctx.Response.Headers["X-Frame-Options"] = "DENY";
		ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
		await next();
	}
);

app.UseResponseCompression();

// Логирование HTTP-запросов (метод, путь, статус, время, IP)
app.UseSerilogRequestLogging(opt =>
{
	opt.MessageTemplate =
		"{RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0}ms [IP: {ClientIp}]";
	opt.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
	{
		diagnosticContext.Set(
			"ClientIp",
			httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
		);
	};
	opt.GetLevel = (httpContext, _, _) =>
		httpContext.Request.Path.StartsWithSegments("/health")
		|| httpContext.Request.Path.StartsWithSegments("/metrics")
			? Serilog.Events.LogEventLevel.Verbose
			: Serilog.Events.LogEventLevel.Information;
});

// Middleware pipeline: ошибки → CORS → rate limit → метрики → аутентификация → авторизация
app.UseExceptionHandler();
app.UseStatusCodePages(async statusCodeContext =>
{
	var httpContext = statusCodeContext.HttpContext;
	if (httpContext.Response.HasStarted)
		return;

	var statusCode = httpContext.Response.StatusCode;
	if (statusCode < StatusCodes.Status400BadRequest)
		return;

	await ApiProblemDetails.WriteAsync(
		httpContext,
		ApiProblemDetails.FromStatus(statusCode, null, httpContext),
		httpContext.RequestAborted
	);
});
app.UseCors("AllowFrontend");
app.UseRateLimiter();
app.UseHttpMetrics();
app.UseAuthentication();
app.UseAuthorization();

// Swagger UI (Scalar) только в dev-режиме
if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
	app.MapScalarApiReference(options =>
	{
		options.Title = "CRM API";
		options.Theme = ScalarTheme.DeepSpace;
		options.DefaultHttpClient = new(ScalarTarget.Shell, ScalarClient.Curl);
		options.AddPreferredSecuritySchemes("Bearer").AddHttpAuthentication("Bearer", _ => { });
	});
}

// Health check эндпоинт (для nginx/k8s/мониторинга)
app.MapHealthChecks("/health");

// Prometheus метрики
app.MapMetrics("/metrics");

app.MapControllers();

app.Run();
