using System.Text;
using CrmWebApi.Data;
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
		var secret =
			config["Jwt:Secret"]
			?? throw new InvalidOperationException("Jwt:Secret не задан в конфигурации");

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
					ValidIssuer = config["Jwt:Issuer"],
					ValidAudience = config["Jwt:Audience"],
					IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
					ClockSkew = TimeSpan.Zero,
				};
			});

		return services;
	}
}
