using System.Threading.RateLimiting;
using CrmWebApi.Common;
using CrmWebApi.Exceptions;
using CrmWebApi.Filters;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace CrmWebApi.Extensions;

public static class ApiConfigurationExtensions
{
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
		}
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
}
