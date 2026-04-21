using System.IO.Compression;
using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.OpenApi;
using Serilog;
using Serilog.Templates;
using Serilog.Templates.Themes;

namespace CrmWebApi.Extensions;

public static class HostConfigurationExtensions
{
	public static ConfigureHostBuilder UseApiSerilog(this ConfigureHostBuilder host)
	{
		host.UseSerilog(
			(context, config) =>
			{
				config
					.WriteTo.Console(
						new ExpressionTemplate(
							"[{@t:HH:mm:ss} {@l:u3}] {Coalesce(Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1), '<no source>')} → {@m}\n{@x}",
							theme: context.HostingEnvironment.IsProduction()
								? null
								: TemplateTheme.Code
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

				if (!context.HostingEnvironment.IsProduction())
					config.WriteTo.Debug();
			}
		);

		return host;
	}

	public static IServiceCollection AddApiOpenApi(this IServiceCollection services)
	{
		services.AddOpenApi(opt =>
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

		return services;
	}

	public static IServiceCollection AddApiResponseCompression(this IServiceCollection services)
	{
		services.AddResponseCompression(opt =>
		{
			opt.EnableForHttps = true;
			opt.Providers.Add<BrotliCompressionProvider>();
			opt.Providers.Add<GzipCompressionProvider>();
		});

		services.Configure<BrotliCompressionProviderOptions>(opt =>
			opt.Level = CompressionLevel.Fastest
		);
		services.Configure<GzipCompressionProviderOptions>(opt =>
			opt.Level = CompressionLevel.Fastest
		);

		return services;
	}

	public static IApplicationBuilder UseApiForwardedHeaders(this IApplicationBuilder app)
	{
		var forwardedOptions = new ForwardedHeadersOptions
		{
			ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
			ForwardLimit = 1
		};

		forwardedOptions.KnownProxies.Add(IPAddress.Parse("127.0.0.1"));
		app.UseForwardedHeaders(forwardedOptions);

		return app;
	}

	public static IApplicationBuilder UseApiSecurityHeaders(this IApplicationBuilder app)
	{
		app.Use(
			async (ctx, next) =>
			{
				ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
				ctx.Response.Headers["X-Frame-Options"] = "DENY";
				ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
				await next();
			}
		);

		return app;
	}

	public static IApplicationBuilder UseApiRequestLogging(this IApplicationBuilder app)
	{
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

		return app;
	}
}
