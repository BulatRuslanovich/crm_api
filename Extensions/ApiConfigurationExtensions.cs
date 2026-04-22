using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;
using CrmWebApi.Common;
using CrmWebApi.Exceptions;
using CrmWebApi.Filters;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace CrmWebApi.Extensions;

public static class ApiConfigurationExtensions
{
	private const string AuthRateLimitIdentityItemKey = "AuthRateLimitIdentity";
	private const int MaxRateLimitBodyBytes = 64 * 1024;

	extension(IServiceCollection services)
	{
		public void AddApiControllers()
		{
			services
				.AddControllers(opt => opt.Filters.Add<ValidationFilter>())
				.AddJsonOptions(opt =>
					opt.JsonSerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonContext.Default)
				);

			services.Configure<ApiBehaviorOptions>(opt =>
			{
				opt.InvalidModelStateResponseFactory = context =>
					ApiProblemDetails.ToActionResult(
						ApiProblemDetails.FromModelState(context.ModelState, context.HttpContext)
					);
			});

			ValidatorOptions.Global.LanguageManager.Culture =
				new System.Globalization.CultureInfo("ru");
			services.AddValidatorsFromAssemblyContaining<ValidationFilter>();
		}

		public void AddApiErrorHandling()
		{
			services.AddProblemDetails();
			services.AddExceptionHandler<DbExceptionHandler>();
			services.AddExceptionHandler<GlobalExceptionHandler>();
		}

		public void AddApiCors(IConfiguration configuration)
		{
			var corsOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

			services.AddCors(options =>
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
		}

		public void AddApiRateLimiting()
		{
			services.AddRateLimiter(options =>
			{
				options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
					RateLimitPartition.GetSlidingWindowLimiter(
						GetGlobalRateLimitKey(context),
						_ => new SlidingWindowRateLimiterOptions
						{
							PermitLimit = 100,
							Window = TimeSpan.FromMinutes(1),
							SegmentsPerWindow = 6,
							QueueLimit = 0,
						}
					)
				);

				options.AddPolicy(
					"auth",
					context =>
						RateLimitPartition.GetFixedWindowLimiter(
							GetAuthRateLimitKey(context),
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
		}
	}

	public static IApplicationBuilder UseApiRateLimitPartitioning(this IApplicationBuilder app)
	{
		app.Use(
			async (ctx, next) =>
			{
				if (ShouldExtractAuthRateLimitIdentity(ctx))
					await ExtractAuthRateLimitIdentityAsync(ctx);

				await next();
			}
		);

		return app;
	}

	public static IApplicationBuilder UseApiErrorHandling(this IApplicationBuilder app)
	{
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

		return app;
	}

	private static string GetGlobalRateLimitKey(HttpContext context)
	{
		if (context.User.Identity?.IsAuthenticated == true)
		{
			var userId =
				context.User.FindFirstValue(ClaimTypes.NameIdentifier)
				?? context.User.FindFirstValue("sub");

			if (!string.IsNullOrWhiteSpace(userId))
				return $"user:{userId}";
		}

		return $"ip:{GetClientIp(context)}";
	}

	private static string GetAuthRateLimitKey(HttpContext context)
	{
		var endpoint = context.Request.Path.Value?.ToLowerInvariant() ?? "/api/auth/unknown";
		var identity =
			context.Items.TryGetValue(AuthRateLimitIdentityItemKey, out var value)
			&& value is string authIdentity
				? authIdentity
				: "anonymous";

		return $"auth:ip:{GetClientIp(context)}:endpoint:{endpoint}:id:{identity}";
	}

	private static string GetClientIp(HttpContext context) =>
		context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

	private static bool ShouldExtractAuthRateLimitIdentity(HttpContext context) =>
		HttpMethods.IsPost(context.Request.Method)
		&& context.Request.Path.StartsWithSegments("/api/auth")
		&& context.Request.HasJsonContentType()
		&& context.Request.ContentLength is null or > 0 and <= MaxRateLimitBodyBytes;

	private static async Task ExtractAuthRateLimitIdentityAsync(HttpContext context)
	{
		context.Request.EnableBuffering(bufferThreshold: 8 * 1024, bufferLimit: MaxRateLimitBodyBytes);

		try
		{
			using var document = await JsonDocument.ParseAsync(context.Request.Body);
			var identity = ExtractIdentity(document.RootElement);

			if (!string.IsNullOrWhiteSpace(identity))
				context.Items[AuthRateLimitIdentityItemKey] = identity;
		}
		catch (JsonException)
		{
			context.Items[AuthRateLimitIdentityItemKey] = "invalid-json";
		}
		catch (IOException)
		{
			context.Items[AuthRateLimitIdentityItemKey] = "oversized-body";
		}
		finally
		{
			if (context.Request.Body.CanSeek)
				context.Request.Body.Position = 0;
		}
	}

	private static string? ExtractIdentity(JsonElement root)
	{
		if (root.ValueKind == JsonValueKind.String)
			return NormalizeIdentity(root.GetString());

		if (root.ValueKind != JsonValueKind.Object)
			return null;

		var email = GetStringProperty(root, "email");
		var login = GetStringProperty(root, "login");

		return (NormalizeIdentity(login), NormalizeIdentity(email)) switch
		{
			({ Length: > 0 } normalizedLogin, { Length: > 0 } normalizedEmail) =>
				$"login:{normalizedLogin}:email:{normalizedEmail}",
			({ Length: > 0 } normalizedLogin, _) => $"login:{normalizedLogin}",
			(_, { Length: > 0 } normalizedEmail) => $"email:{normalizedEmail}",
			_ => null,
		};
	}

	private static string? GetStringProperty(JsonElement root, string propertyName)
	{
		foreach (var property in root.EnumerateObject())
		{
			if (!property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
				continue;

			return property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() : null;
		}

		return null;
	}

	private static string? NormalizeIdentity(string? value)
	{
		var normalized = value?.Trim().ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(normalized))
			return null;

		return normalized.Length <= 128 ? normalized : normalized[..128];
	}
}
