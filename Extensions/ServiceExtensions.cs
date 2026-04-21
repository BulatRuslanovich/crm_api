using System.Text;
using CrmWebApi.Common;
using CrmWebApi.Data;
using CrmWebApi.Options;
using CrmWebApi.Repositories;
using CrmWebApi.Repositories.Impl;
using CrmWebApi.Services;
using CrmWebApi.Services.Impl;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CrmWebApi.Extensions;

public static class ServiceExtensions
{
	public static IServiceCollection AddRepositories(this IServiceCollection services)
	{
		services.AddScoped<IRefreshRepository, RefreshRepository>();
		services.AddScoped<IEmailTokenRepository, EmailTokenRepository>();
		services.AddScoped<IUserRepository, UserRepository>();
		services.AddScoped<IOrgRepository, OrgRepository>();
		services.AddScoped<IPhysRepository, PhysRepository>();
		services.AddScoped<IActivRepository, ActivRepository>();
		services.AddScoped<IDepartmentRepository, DepartmentRepository>();
		return services;
	}

	public static IServiceCollection AddServices(this IServiceCollection services)
	{
		services.AddScoped<IUserService, UserService>();
		services.AddScoped<IAuthService, AuthService>();
		services.AddScoped<IEmailService, EmailService>();
		services.AddScoped<IDrugService, DrugService>();
		services.AddScoped<IOrgService, OrgService>();
		services.AddScoped<IPhysService, PhysService>();
		services.AddScoped<IActivService, ActivService>();
		services.AddScoped<IDepartmentService, DepartmentService>();
		return services;
	}

	public static IServiceCollection AddApiCaching(
		this IServiceCollection services,
		IConfiguration config
	)
	{
		var cacheOptions = config.GetSection(CacheOptions.SectionName).Get<CacheOptions>() ?? new();
		if (!string.IsNullOrWhiteSpace(cacheOptions.RedisConnectionString))
		{
			services.AddStackExchangeRedisCache(opt =>
			{
				opt.Configuration = cacheOptions.RedisConnectionString;
				opt.InstanceName = "crm-api:";
			});
		}

		services.AddHybridCache(opt =>
		{
			opt.DefaultEntryOptions = new() { Expiration = TimeSpan.FromMinutes(1) };
		});

		return services;
	}

	public static IServiceCollection AddApiOptions(
		this IServiceCollection services,
		IConfiguration config
	)
	{
		services
			.AddOptions<JwtOptions>()
			.Bind(config.GetSection(JwtOptions.SectionName))
			.Validate(JwtOptions.HasValidSecret, "Jwt:Secret must be at least 32 characters and cannot be a placeholder")
			.Validate(
				o => !string.IsNullOrWhiteSpace(o.Issuer),
				"Jwt:Issuer must be configured"
			)
			.Validate(
				o => !string.IsNullOrWhiteSpace(o.Audience),
				"Jwt:Audience must be configured"
			)
			.Validate(o => o.AccessTokenTtlMinutes > 0, "Jwt:AccessTokenTtlMinutes must be positive")
			.Validate(o => o.RefreshTokenTtlDays > 0, "Jwt:RefreshTokenTtlDays must be positive")
			.ValidateOnStart();

		services
			.AddOptions<AuthOptions>()
			.Bind(config.GetSection(AuthOptions.SectionName))
			.Validate(
				o =>
					string.IsNullOrWhiteSpace(o.OtpHashSecret)
					|| o.OtpHashSecret.Length >= 32,
				"Auth:OtpHashSecret must be at least 32 characters when configured"
			)
			.ValidateOnStart();

		services.AddOptions<EmailOptions>().Bind(config.GetSection(EmailOptions.SectionName));
		services.AddOptions<CacheOptions>().Bind(config.GetSection(CacheOptions.SectionName));

		return services;
	}

	public static IServiceCollection AddDatabase(
		this IServiceCollection services,
		IConfiguration config
	)
	{
		services.AddDbContextPool<AppDbContext>(opt =>
			opt.UseNpgsql(config.GetConnectionString("Default"))
		);
		return services;
	}

	public static IServiceCollection AddJwt(this IServiceCollection services, IConfiguration config)
	{
		var jwtOptions =
			config.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
			?? throw new InvalidOperationException("Jwt не задан в конфигурации");

		services
			.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
			.AddJwtBearer(opt =>
			{
				opt.TokenValidationParameters = new TokenValidationParameters
				{
					ValidateIssuer = true,
					ValidateAudience = true,
					ValidateLifetime = true,
					ValidateIssuerSigningKey = true,
					ValidIssuer = jwtOptions.Issuer,
					ValidAudience = jwtOptions.Audience,
					IssuerSigningKey = new SymmetricSecurityKey(
						Encoding.UTF8.GetBytes(jwtOptions.Secret)
					),
					ClockSkew = TimeSpan.Zero,
				};

				opt.Events = new JwtBearerEvents
				{
					OnChallenge = async context =>
					{
						if (context.Response.HasStarted)
							return;

						context.HandleResponse();
						await ApiProblemDetails.WriteAsync(
							context.HttpContext,
							ApiProblemDetails.FromStatus(
								StatusCodes.Status401Unauthorized,
								"Требуется авторизация",
								context.HttpContext
							)
						);
					},
					OnForbidden = async context =>
					{
						if (context.Response.HasStarted)
							return;

						await ApiProblemDetails.WriteAsync(
							context.HttpContext,
							ApiProblemDetails.FromStatus(
								StatusCodes.Status403Forbidden,
								"Доступ запрещён",
								context.HttpContext
							)
						);
					},
				};
			});

		return services;
	}
}
