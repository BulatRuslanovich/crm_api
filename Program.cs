using CrmWebApi.Extensions;
using Prometheus;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseApiSerilog();

// Гибридный кеш (in-memory + distributed), TTL 1 минута
builder.Services.AddHybridCache(opt =>
{
	opt.DefaultEntryOptions = new() { Expiration = TimeSpan.FromMinutes(1) };
});

builder.Services.AddApiControllers();

// Регистрация бизнес-сервисов (extension method)
builder.Services.AddServices();

// OpenAPI-документация с JWT Bearer схемой авторизации
builder.Services.AddApiOpenApi();

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

builder.Services.AddApiErrorHandling();
builder.Services.AddApiCors(builder.Configuration);
builder.Services.AddApiRateLimiting();

builder.Services.AddApiResponseCompression();

var app = builder.Build();

app.UseApiForwardedHeaders();

// HTTPS-редирект и HSTS первыми — до любой обработки контента
if (app.Environment.IsProduction())
{
	app.UseHttpsRedirection();
	app.UseHsts();
}

app.UseApiSecurityHeaders();
app.UseResponseCompression();
app.UseApiRequestLogging();

// Middleware pipeline: ошибки → CORS → rate limit → метрики → аутентификация → авторизация
app.UseApiErrorHandling();
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
