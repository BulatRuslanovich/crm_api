using System.Text;
using CrmWebApi.Common;
using CrmWebApi.Data;
using CrmWebApi.Options;
using CrmWebApi.Repositories;
using CrmWebApi.Repositories.Impl;
using CrmWebApi.Services;
using CrmWebApi.Services.Assistant;
using CrmWebApi.Services.Assistant.Providers;
using CrmWebApi.Services.Assistant.Tools;
using CrmWebApi.Services.Impl;
using CrmWebApi.Services.Impl.Assistant;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CrmWebApi.Extensions;

public static class ServiceExtensions
{
	extension(IServiceCollection services)
	{
		public void AddRepositories()
		{
			services.AddScoped<IRefreshRepository, RefreshRepository>();
			services.AddScoped<IEmailTokenRepository, EmailTokenRepository>();
			services.AddScoped<IUserRepository, UserRepository>();
			services.AddScoped<IOrgRepository, OrgRepository>();
			services.AddScoped<IPhysRepository, PhysRepository>();
			services.AddScoped<IActivRepository, ActivRepository>();
			services.AddScoped<IDepartmentRepository, DepartmentRepository>();
		}

		public void AddServices()
		{
			services.AddHttpContextAccessor();
			services.AddScoped<ICurrentUserService, CurrentUserService>();
			services.AddScoped<IAuditService, AuditService>();
			services.AddScoped<IUserService, UserService>();
			services.AddScoped<IAuthService, AuthService>();
			services.AddScoped<IAuthSessionService, AuthSessionService>();
			services.AddScoped<IEmailOtpService, EmailOtpService>();
			services.AddSingleton<IPasswordHasher, PasswordHasher>();
			services.AddScoped<IEmailService, EmailService>();
			services.AddScoped<IDrugService, DrugService>();
			services.AddScoped<IOrgService, OrgService>();
			services.AddScoped<IPhysService, PhysService>();
			services.AddScoped<IActivService, ActivService>();
			services.AddScoped<IDepartmentService, DepartmentService>();
		}

		public void AddAssistantFeature(IConfiguration config)
		{
			var section = config.GetSection(AssistantOptions.SectionName);
			services.AddOptions<AssistantOptions>().Bind(section);

			var opts = section.Get<AssistantOptions>() ?? new AssistantOptions();
			if (!opts.Enabled)
			{
				services.AddScoped<IAssistantService, DisabledAssistantService>();
				return;
			}

			if (string.IsNullOrWhiteSpace(opts.Cloud.BaseUrl))
				throw new InvalidOperationException("Assistant:Cloud:BaseUrl is empty.");
			if (string.IsNullOrWhiteSpace(opts.Cloud.Model))
				throw new InvalidOperationException("Assistant:Cloud:Model is empty.");
			if (string.IsNullOrWhiteSpace(opts.Cloud.ApiKey))
				throw new InvalidOperationException(
					"Assistant:Cloud:ApiKey is empty. Set it via user-secrets or environment variable.");

			services.AddHttpClient<OpenAiCompatibleProvider>(client =>
			{
				var baseUrl = opts.Cloud.BaseUrl.EndsWith('/') ? opts.Cloud.BaseUrl : opts.Cloud.BaseUrl + "/";
				client.BaseAddress = new Uri(baseUrl);
				if (!string.IsNullOrWhiteSpace(opts.Cloud.ApiKey))
				{
					client.DefaultRequestHeaders.Authorization =
						new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", opts.Cloud.ApiKey);
				}
				client.Timeout = Timeout.InfiniteTimeSpan;
			});

			services.AddScoped<IChatProvider>(sp => sp.GetRequiredService<OpenAiCompatibleProvider>());
			services.AddScoped<IAssistantCrmReadPort, AssistantCrmPort>();
			services.AddScoped<IAssistantCrmWritePort, AssistantCrmPort>();
			services.AddScoped<IAssistantTool, SearchDrugsTool>();
			services.AddScoped<IAssistantTool, SearchPhysesTool>();
			services.AddScoped<IAssistantTool, SearchOrgsTool>();
			services.AddScoped<IAssistantTool, GetDrugDetailsTool>();
			services.AddScoped<IAssistantTool, GetPhysDetailsTool>();
			services.AddScoped<IAssistantTool, GetOrgDetailsTool>();
			services.AddScoped<IAssistantTool, ListActivsTool>();
			services.AddScoped<IAssistantTool, GetActivDetailsTool>();
			services.AddScoped<IAssistantTool, ProposeCreateActivTool>();
			services.AddScoped<IAssistantTool, SearchUiHelpTool>();
			services.AddSingleton<IAssistantActionStore, InMemoryAssistantActionStore>();
			services.AddScoped<IAssistantService, AssistantService>();
		}

		public void AddApiOptions(IConfiguration config)
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
			services
				.AddOptions<ApiForwardedHeadersOptions>()
				.Bind(config.GetSection(ApiForwardedHeadersOptions.SectionName))
				.Validate(o => o.ForwardLimit > 0, "ForwardedHeaders:ForwardLimit must be positive")
				.ValidateOnStart();
		}

		public void AddDatabase(IConfiguration config)
		{
			services.AddDbContextPool<AppDbContext>(opt =>
				opt.UseNpgsql(config.GetConnectionString("Default"))
			);
		}

		public void AddJwt(IConfiguration config)
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
		}
	}
}
