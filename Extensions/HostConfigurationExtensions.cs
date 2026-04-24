using System.IO.Compression;
using System.Net;
using CrmWebApi.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;
using Serilog.Templates;
using Serilog.Templates.Themes;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace CrmWebApi.Extensions;

public static class HostConfigurationExtensions
{
	private const string ConsoleTemplate =
		"[{@t:HH:mm:ss} {@l:u3}] {Coalesce(Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1), '<no source>')} → {@m}\n{@x}";

	private static readonly Dictionary<string, LogEventLevel> Overrides = new()
	{
		["Microsoft.AspNetCore.Hosting"] = LogEventLevel.Warning,
		["Microsoft.AspNetCore.Routing"] = LogEventLevel.Warning,
		["Microsoft.AspNetCore.Diagnostics"] = LogEventLevel.Warning,
		["Microsoft.AspNetCore.Authorization"] = LogEventLevel.Warning,
		["Microsoft.AspNetCore.Authentication"] = LogEventLevel.Warning,
		["Microsoft.AspNetCore.Mvc.Infrastructure.ObjectResultExecutor"] = LogEventLevel.Warning,
		["Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker"] = LogEventLevel.Warning,
		// Раскомментируй, когда созреешь:
		// ["Microsoft.EntityFrameworkCore.Database.Command"] = LogEventLevel.Warning,
	};


	public static void UseApiSerilog(this ConfigureHostBuilder builder)
	{
		builder.UseSerilog(
			(context, config) =>
			{
				var isProduction = context.HostingEnvironment.IsProduction();

				config
					.WriteTo.Console(CreateConsoleTemplate(isProduction))
					.MinimumLevel.Information()
					.ApplyOverrides();

				if (!isProduction)
					config.WriteTo.Debug();
			}
		);
	}

	private static ExpressionTemplate CreateConsoleTemplate(bool isProduction)
	{
		return new ExpressionTemplate(
			ConsoleTemplate,
			theme: isProduction ? null : TemplateTheme.Code
		);
	}

	private static void ApplyOverrides(this LoggerConfiguration config)
	{
		foreach (var (source, level) in Overrides)
		{
			config.MinimumLevel.Override(source, level);
		}
	}

	extension(IServiceCollection services)
	{
		public void AddCustomSwagger()
		{
			services.AddOpenApi(opt =>
			{
				opt.AddDocumentTransformer(
					(document, _, _) =>
					{
						document.Info = new OpenApiInfo
						{
							Title = "PHARMO API",
							Version = "v1",
							Description = """
								## Overview
								REST API for PHARMO.

								## Authentication
								Most endpoints require a Bearer JWT token. Obtain a token via `POST /api/auth/login`, then click **Authorize** and paste:

								`Bearer <your_token>`
								""",
						};

						var components = document.Components ?? new OpenApiComponents();
						components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
						components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
						{
							Type = SecuritySchemeType.Http,
							Scheme = "bearer",
							BearerFormat = "JWT",
							Description = """
								JWT access token issued by `POST /api/auth/login`.

								Header format:
								`Authorization: Bearer eyJhbGci...`
								""",
						};
						document.Components = components;

						return Task.CompletedTask;
					}
				);

				opt.AddOperationTransformer(
					(operation, context, _) =>
					{
						var metadata = context.Description.ActionDescriptor.EndpointMetadata;
						var allowAnonymous = metadata.OfType<IAllowAnonymous>().Any();
						var authorize = metadata.OfType<IAuthorizeData>().Any();
						operation.Responses ??= [];

						if (allowAnonymous)
						{
							operation.Security = new List<OpenApiSecurityRequirement>();
							return Task.CompletedTask;
						}

						if (authorize)
						{
							operation.Security = new List<OpenApiSecurityRequirement>
							{
								new() { [new OpenApiSecuritySchemeReference("Bearer")] = [], }
							};
						}

						operation.Responses.TryAdd(
							"401",
							new OpenApiResponse
							{
								Description = "Unauthorized: missing, expired or invalid JWT token.",
							}
						);
						operation.Responses.TryAdd(
							"403",
							new OpenApiResponse
							{
								Description = "Forbidden: authenticated user does not have the required role or policy.",
							}
						);

						return Task.CompletedTask;
					}
				);
			});
		}

		public void AddApiResponseCompression()
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
		}
	}

	extension(WebApplication app)
	{
		public void UseApiDocs()
		{
			app.MapOpenApi();

			app.MapScalarApiReference(options =>
			{
				options.Title = "PHARMO API";
				options.Theme = ScalarTheme.DeepSpace;
				options.DefaultHttpClient = new KeyValuePair<ScalarTarget, ScalarClient>(
					ScalarTarget.CSharp,
					ScalarClient.HttpClient
				);
				options
					.AddPreferredSecuritySchemes("Bearer")
					.AddHttpAuthentication("Bearer", _ => { });
			});

			app.UseSwaggerUI(options =>
			{
				options.RoutePrefix = "swagger";
				options.SwaggerEndpoint("/openapi/v1.json", "PHARMO API v1");
				options.DocumentTitle = "PHARMO API - Swagger UI";
				options.DocExpansion(DocExpansion.List);
				options.DefaultModelRendering(ModelRendering.Example);
				options.DefaultModelExpandDepth(3);
				options.DefaultModelsExpandDepth(1);
				options.DisplayOperationId();
				options.DisplayRequestDuration();
				options.EnableDeepLinking();
				options.EnableFilter();
				options.EnablePersistAuthorization();
				options.EnableTryItOutByDefault();
				options.ShowCommonExtensions();
				options.ShowExtensions();
			});
		}
	}

	extension(IApplicationBuilder app)
	{
		public void UseApiCompressionGuards()
		{
			app.Use(
				async (ctx, next) =>
				{
					if (ctx.Request.Path.StartsWithSegments("/api/auth"))
						ctx.Request.Headers.Remove("Accept-Encoding");

					await next();
				}
			);
		}

		public void UseApiForwardedHeaders()
		{
			var configuredOptions = app
				.ApplicationServices.GetRequiredService<IOptions<ApiForwardedHeadersOptions>>()
				.Value;

			var forwardedOptions = new ForwardedHeadersOptions
			{
				ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
				ForwardLimit = configuredOptions.ForwardLimit
			};

			foreach (var proxy in configuredOptions.KnownProxies)
			{
				if (!IPAddress.TryParse(proxy, out var ip))
					throw new InvalidOperationException($"Invalid ForwardedHeaders:KnownProxies value: {proxy}");

				forwardedOptions.KnownProxies.Add(ip);
			}

			foreach (var network in configuredOptions.KnownNetworks)
			{
				if (!System.Net.IPNetwork.TryParse(network, out var ipNetwork))
					throw new InvalidOperationException($"Invalid ForwardedHeaders:KnownNetworks value: {network}");

				forwardedOptions.KnownIPNetworks.Add(ipNetwork);
			}

			app.UseForwardedHeaders(forwardedOptions);
		}

		public void UseApiSecurityHeaders()
		{
			app.Use(
				async (ctx, next) =>
				{
					ctx.Response.Headers.XContentTypeOptions = "nosniff";
					ctx.Response.Headers.XFrameOptions = "DENY";
					ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
					await next();
				}
			);
		}

		public void UseApiRequestLogging()
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
						? LogEventLevel.Verbose
						: httpContext.Response.StatusCode >= 400
							? LogEventLevel.Warning
							: LogEventLevel.Information;
			});
		}
	}
}
