using CrmWebApi.Common;
using CrmWebApi.Data;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Auth;
using CrmWebApi.DTOs.Drug;
using CrmWebApi.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CrmWebApi.Tests;

public sealed class ApiTestFactory : WebApplicationFactory<Program>
{
	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		builder.UseEnvironment("Testing");
		builder.UseSetting("Jwt:Secret", "integration-test-secret-with-more-than-32-chars");
		builder.UseSetting("Jwt:Issuer", "PharmaCrmApi");
		builder.UseSetting("Jwt:Audience", "PharmaCrmClient");
		builder.UseSetting("ConnectionStrings:Default", "Host=localhost;Database=test");
		builder.UseSetting("Email:Host", "localhost");
		builder.UseSetting("Email:Username", "test");
		builder.UseSetting("Email:Password", "test");
		builder.UseSetting("Email:FromAddress", "test@example.com");

		builder.ConfigureServices(services =>
		{
			services.RemoveAll<DbContextOptions<AppDbContext>>();
			services.RemoveAll<AppDbContext>();
			services.RemoveAll<IDrugService>();
			services.RemoveAll<IAuthService>();
			services.RemoveAll<IHealthCheck>();

			services.AddDbContextPool<AppDbContext>(opt =>
				opt.UseInMemoryDatabase($"crm-api-tests-{Guid.NewGuid()}")
			);
			services.AddSingleton<IDrugService, FakeDrugService>();
			services.AddSingleton<IAuthService, FakeAuthService>();
			services.AddSingleton<IHealthCheck, HealthyHealthCheck>();
			services.PostConfigure<HealthCheckServiceOptions>(opt =>
			{
				opt.Registrations.Clear();
				opt.Registrations.Add(
					new HealthCheckRegistration(
						"self",
						_ => new HealthyHealthCheck(),
						HealthStatus.Unhealthy,
						tags: null
					)
				);
			});
		});
	}

	private sealed class FakeDrugService : IDrugService
	{
		private static readonly List<DrugResponse> Drugs =
		[
			new(1, "Амоксициллин", "Флемоксин", "таблетки"),
			new(2, "Ибупрофен", "Нурофен", "капсулы"),
			new(3, "Парацетамол", "Панадол", "таблетки"),
		];

		public Task<Result<PagedResponse<DrugResponse>>> GetAllAsync(
			int page,
			int pageSize,
			string? search,
			bool includeTotal = true
		)
		{
			var query = string.IsNullOrWhiteSpace(search)
				? Drugs
				: Drugs
					.Where(d => d.DrugName.Contains(search, StringComparison.OrdinalIgnoreCase))
					.ToList();
			var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
			var total = includeTotal ? query.Count : 0;

			return Task.FromResult<Result<PagedResponse<DrugResponse>>>(
				new PagedResponse<DrugResponse>(items, page, pageSize, total)
			);
		}

		public Task<Result<DrugResponse>> GetByIdAsync(int id)
		{
			var drug = Drugs.FirstOrDefault(d => d.DrugId == id);
			return Task.FromResult<Result<DrugResponse>>(
				drug is null ? Error.NotFound($"Препарат {id} не найден") : drug
			);
		}

		public Task<Result<DrugResponse>> CreateAsync(CreateDrugRequest req) =>
			Task.FromResult<Result<DrugResponse>>(new DrugResponse(10, req.DrugName, req.Brand, req.Form));

		public Task<Result<DrugResponse>> UpdateAsync(int id, UpdateDrugRequest req) =>
			GetByIdAsync(id);

		public Task<Result> DeleteAsync(int id) => Task.FromResult(Result.Success());
	}

	private sealed class FakeAuthService : IAuthService
	{
		public Task<Result<PendingConfirmationResponse>> RegisterAsync(RegisterRequest req) =>
			Task.FromResult<Result<PendingConfirmationResponse>>(
				new PendingConfirmationResponse(req.Email)
			);

		public Task<Result<AuthTokens>> ConfirmEmailAsync(ConfirmEmailRequest req) =>
			Task.FromResult<Result<AuthTokens>>(Error.Validation("Неверный или истёкший код"));

		public Task<Result> ResendConfirmationAsync(string email) =>
			Task.FromResult(Result.Success());

		public Task<Result<AuthTokens>> LoginAsync(LoginRequest req) =>
			Task.FromResult<Result<AuthTokens>>(Error.Unauthorized("Неверный логин или пароль"));

		public Task<Result<AuthTokens>> RefreshAsync(string refreshToken) =>
			Task.FromResult<Result<AuthTokens>>(
				Error.Unauthorized("Refresh token не найден или уже использован")
			);

		public Task<Result> LogoutAsync(string refreshToken) =>
			Task.FromResult(Result.Success());

		public Task<Result> ForgotPasswordAsync(string email) =>
			Task.FromResult(Result.Success());

		public Task<Result> ResetPasswordAsync(ResetPasswordRequest req) =>
			Task.FromResult(Result.Success());
	}

	private sealed class HealthyHealthCheck : IHealthCheck
	{
		public Task<HealthCheckResult> CheckHealthAsync(
			HealthCheckContext context,
			CancellationToken cancellationToken = default
		) => Task.FromResult(HealthCheckResult.Healthy());
	}
}
