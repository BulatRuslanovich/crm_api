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
using Serilog.Templates;
using Serilog.Templates.Themes;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace CrmWebApi.Extensions;

public static class HostConfigurationExtensions
{
	public static void UseApiSerilog(this ConfigureHostBuilder host)
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
	}

	extension(IServiceCollection services)
	{
		public void AddApiOpenApi()
		{
			services.AddOpenApi(opt =>
			{
				opt.AddDocumentTransformer(
					(document, _, _) =>
					{
						document.Info = new OpenApiInfo
						{
							Title = "PHARMO CRM API",
							Version = "v1",
							Description = """
								## Overview
								REST API for PHARMO CRM, a pharmaceutical customer relationship management platform.

								## Authentication
								Most endpoints require a Bearer JWT token. Obtain a token via `POST /api/auth/login`, then click **Authorize** and paste:

								`Bearer <your_token>`

								## Rate Limiting
								Auth endpoints are limited more aggressively than the rest of the API. When a limit is exceeded, the API returns `429 Too Many Requests`.

								## Local Development
								The default local API URL is `http://localhost:5000`.
								""",
						};

						document.Servers =
						[
							new OpenApiServer
							{
								Url = "http://localhost:5000",
								Description = "Local development",
							},
						];

						document.Tags = new HashSet<OpenApiTag>
						{
							new() { Name = "Auth", Description = "Registration, login, email confirmation, password reset, refresh and logout." },
							new() { Name = "Users", Description = "User accounts, current profile and access policies." },
							new() { Name = "Departments", Description = "Departments and department membership." },
							new() { Name = "Orgs", Description = "Organizations and organization types." },
							new() { Name = "Physes", Description = "Contacts, specializations and organization links." },
							new() { Name = "Drugs", Description = "Drug catalog management." },
							new() { Name = "Activs", Description = "CRM activities and linked drugs." },
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

						document.Security =
						[
							new OpenApiSecurityRequirement
							{
								[new OpenApiSecuritySchemeReference("Bearer")] = [],
							}
						];

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

						if (!allowAnonymous && authorize)
						{
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
						}

						operation.Responses.TryAdd(
							"429",
							new OpenApiResponse
							{
								Description = "Too Many Requests: rate limit exceeded.",
							}
						);

						var sortedResponses = operation.Responses
							.OrderBy(response => response.Key, StringComparer.Ordinal)
							.ToArray();

						operation.Responses.Clear();
						foreach (var (statusCode, response) in sortedResponses)
							operation.Responses.Add(statusCode, response);

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
						? Serilog.Events.LogEventLevel.Verbose
						: Serilog.Events.LogEventLevel.Information;
			});
		}
	}
}
