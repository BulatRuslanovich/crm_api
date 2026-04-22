using CrmWebApi.Common;
using CrmWebApi.Data;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Activ;
using CrmWebApi.DTOs.Auth;
using CrmWebApi.DTOs.Department;
using CrmWebApi.DTOs.Drug;
using CrmWebApi.DTOs.Org;
using CrmWebApi.DTOs.OrgType;
using CrmWebApi.DTOs.Phys;
using CrmWebApi.DTOs.Policy;
using CrmWebApi.DTOs.Spec;
using CrmWebApi.DTOs.User;
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
			services.RemoveAll<IAuthService>();
			services.RemoveAll<IActivService>();
			services.RemoveAll<IDepartmentService>();
			services.RemoveAll<IDrugService>();
			services.RemoveAll<IOrgService>();
			services.RemoveAll<IPhysService>();
			services.RemoveAll<IUserService>();
			services.RemoveAll<IHealthCheck>();

			services.AddDbContextPool<AppDbContext>(opt =>
				opt.UseInMemoryDatabase($"crm-api-tests-{Guid.NewGuid()}")
			);
			services.AddSingleton<IAuthService, FakeAuthService>();
			services.AddSingleton<IActivService, FakeActivService>();
			services.AddSingleton<IDepartmentService, FakeDepartmentService>();
			services.AddSingleton<IDrugService, FakeDrugService>();
			services.AddSingleton<IOrgService, FakeOrgService>();
			services.AddSingleton<IPhysService, FakePhysService>();
			services.AddSingleton<IUserService, FakeUserService>();
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

	private sealed class FakeOrgService : IOrgService
	{
		private static readonly List<OrgResponse> Orgs =
		[
			new(1, 1, "Аптека", "Аптека N1", "7701000001", 55.75, 37.61, "Москва"),
		];

		public Task<Result<PagedResponse<OrgResponse>>> GetAllAsync(
			int page,
			int pageSize,
			string? search,
			bool includeTotal = true
		) =>
			Task.FromResult<Result<PagedResponse<OrgResponse>>>(
				new PagedResponse<OrgResponse>(Orgs, page, pageSize, includeTotal ? Orgs.Count : 0)
			);

		public Task<Result<OrgResponse>> GetByIdAsync(int id) =>
			Task.FromResult(
				Orgs.FirstOrDefault(o => o.OrgId == id) is { } org
					? Result<OrgResponse>.Success(org)
					: Error.NotFound($"Организация {id} не найдена")
			);

		public Task<Result<OrgResponse>> CreateAsync(CreateOrgRequest req) =>
			Task.FromResult<Result<OrgResponse>>(
				new OrgResponse(
					10,
					req.OrgTypeId,
					"Аптека",
					req.OrgName,
					req.Inn,
					req.Latitude,
					req.Longitude,
					req.Address
				)
			);

		public Task<Result<OrgResponse>> UpdateAsync(int id, UpdateOrgRequest req) => GetByIdAsync(id);

		public Task<Result> DeleteAsync(int id) => Task.FromResult(Result.Success());

		public Task<Result<IEnumerable<OrgTypeResponse>>> GetAllTypesAsync() =>
			Task.FromResult<Result<IEnumerable<OrgTypeResponse>>>(
				new List<OrgTypeResponse> { new(1, "Аптека") }
			);
	}

	private sealed class FakePhysService : IPhysService
	{
		private static readonly List<PhysResponse> Physes =
		[
			new(
				1,
				1,
				"Терапевт",
				"Иван",
				"Иванов",
				"Иванович",
				"+70000000000",
				"phys@example.com",
				[]
			),
		];

		public Task<Result<PagedResponse<PhysResponse>>> GetAllAsync(
			int page,
			int pageSize,
			string? search,
			bool includeTotal = true
		) =>
			Task.FromResult<Result<PagedResponse<PhysResponse>>>(
				new PagedResponse<PhysResponse>(Physes, page, pageSize, includeTotal ? Physes.Count : 0)
			);

		public Task<Result<PhysResponse>> GetByIdAsync(int id) =>
			Task.FromResult(
				Physes.FirstOrDefault(p => p.PhysId == id) is { } phys
					? Result<PhysResponse>.Success(phys)
					: Error.NotFound($"Физическое лицо {id} не найдено")
			);

		public Task<Result<PhysResponse>> CreateAsync(CreatePhysRequest req) =>
			Task.FromResult<Result<PhysResponse>>(
				new PhysResponse(
					10,
					req.SpecId,
					"Терапевт",
					req.FirstName,
					req.LastName,
					req.MiddleName,
					req.Phone,
					req.Email,
					[]
				)
			);

		public Task<Result<PhysResponse>> UpdateAsync(int id, UpdatePhysRequest req) => GetByIdAsync(id);

		public Task<Result> DeleteAsync(int id) => Task.FromResult(Result.Success());

		public Task<Result> LinkOrgAsync(int physId, int orgId) => Task.FromResult(Result.Success());

		public Task<Result> UnlinkOrgAsync(int physId, int orgId) => Task.FromResult(Result.Success());

		public Task<Result<IEnumerable<SpecResponse>>> GetAllSpecsAsync() =>
			Task.FromResult<Result<IEnumerable<SpecResponse>>>(
				new List<SpecResponse> { new(1, "Терапевт") }
			);

		public Task<Result<SpecResponse>> GetSpecByIdAsync(int id) =>
			Task.FromResult<Result<SpecResponse>>(new SpecResponse(id, "Терапевт"));

		public Task<Result<SpecResponse>> CreateSpecAsync(CreateSpecRequest req) =>
			Task.FromResult<Result<SpecResponse>>(new SpecResponse(10, req.SpecName));

		public Task<Result> DeleteSpecAsync(int id) => Task.FromResult(Result.Success());
	}

	private sealed class FakeActivService : IActivService
	{
		private static readonly List<ActivResponse> Activs =
		[
			new(
				1,
				1,
				"admin",
				1,
				"Аптека N1",
				1,
				"Иванов Иван Иванович",
				1,
				"Запланировано",
				DateTimeOffset.UtcNow,
				DateTimeOffset.UtcNow.AddHours(1),
				"Визит",
				[]
			),
		];

		public Task<Result<PagedResponse<ActivResponse>>> GetAllAsync(ActivQuery query, Scope scope) =>
			Task.FromResult<Result<PagedResponse<ActivResponse>>>(
				new PagedResponse<ActivResponse>(Activs, query.Page, query.PageSize, Activs.Count)
			);

		public Task<Result<ActivResponse>> GetByIdAsync(int id, Scope scope) =>
			Task.FromResult(
				Activs.FirstOrDefault(a => a.ActivId == id) is { } activ
					? Result<ActivResponse>.Success(activ)
					: Error.NotFound($"Активность {id} не найдена")
			);

		public Task<Result<ActivResponse>> CreateAsync(int usrId, CreateActivRequest req) =>
			Task.FromResult<Result<ActivResponse>>(Activs[0] with { UsrId = usrId });

		public Task<Result<ActivResponse>> UpdateAsync(int id, UpdateActivRequest req, Scope scope) =>
			GetByIdAsync(id, scope);

		public Task<Result> DeleteAsync(int id, Scope scope) => Task.FromResult(Result.Success());

		public Task<Result> LinkDrugAsync(int activId, int drugId, Scope scope) =>
			Task.FromResult(Result.Success());

		public Task<Result> UnlinkDrugAsync(int activId, int drugId, Scope scope) =>
			Task.FromResult(Result.Success());
	}

	private sealed class FakeUserService : IUserService
	{
		private static readonly List<UserResponse> Users =
		[
			new(1, "Admin", "User", "admin@example.com", "admin", [RoleNames.Admin]),
		];

		public Task<Result<PagedResponse<UserResponse>>> GetAllAsync(
			int page,
			int pageSize,
			Scope scope,
			bool includeTotal = true
		) =>
			Task.FromResult<Result<PagedResponse<UserResponse>>>(
				new PagedResponse<UserResponse>(Users, page, pageSize, includeTotal ? Users.Count : 0)
			);

		public Task<Result<UserResponse>> GetByIdAsync(int id) =>
			Task.FromResult(
				Users.FirstOrDefault(u => u.UsrId == id) is { } user
					? Result<UserResponse>.Success(user)
					: Error.NotFound($"Пользователь {id} не найден")
			);

		public Task<Result<UserResponse>> CreateAsync(CreateUserRequest request) =>
			Task.FromResult<Result<UserResponse>>(
				new UserResponse(10, request.FirstName, request.LastName, request.Email, request.Login, [])
			);

		public Task<Result<UserResponse>> UpdateAsync(int id, UpdateUserRequest request) => GetByIdAsync(id);

		public Task<Result> DeleteAsync(int id) => Task.FromResult(Result.Success());

		public Task<Result> ChangePasswordAsync(int id, ChangePasswordRequest request) =>
			Task.FromResult(Result.Success());

		public Task<Result<UserResponse>> LinkPolicyAsync(int userId, int policyId) => GetByIdAsync(userId);

		public Task<Result<UserResponse>> UnlinkPolicyAsync(int userId, int policyId) => GetByIdAsync(userId);

		public Task<Result<IEnumerable<PolicyResponse>>> GetAllPoliciesAsync() =>
			Task.FromResult<Result<IEnumerable<PolicyResponse>>>(
				new List<PolicyResponse> { new(1, RoleNames.Admin) }
			);

		public Task<Result<PolicyResponse>> GetPolicyByIdAsync(int id) =>
			Task.FromResult<Result<PolicyResponse>>(new PolicyResponse(id, RoleNames.Admin));
	}

	private sealed class FakeDepartmentService : IDepartmentService
	{
		private static readonly List<DepartmentResponse> Departments =
		[
			new(1, "Продажи", 3),
		];

		public Task<Result<PagedResponse<DepartmentResponse>>> GetAllAsync(
			int page,
			int pageSize,
			bool includeTotal = true
		) =>
			Task.FromResult<Result<PagedResponse<DepartmentResponse>>>(
				new PagedResponse<DepartmentResponse>(
					Departments,
					page,
					pageSize,
					includeTotal ? Departments.Count : 0
				)
			);

		public Task<Result<DepartmentResponse>> GetByIdAsync(int id) =>
			Task.FromResult(
				Departments.FirstOrDefault(d => d.DepartmentId == id) is { } department
					? Result<DepartmentResponse>.Success(department)
					: Error.NotFound($"Отдел {id} не найден")
			);

		public Task<Result<DepartmentResponse>> CreateAsync(CreateDepartmentRequest req) =>
			Task.FromResult<Result<DepartmentResponse>>(
				new DepartmentResponse(10, req.DepartmentName, 0)
			);

		public Task<Result> DeleteAsync(int id) => Task.FromResult(Result.Success());

		public Task<Result> AddUserAsync(int departmentId, int usrId) =>
			Task.FromResult(Result.Success());

		public Task<Result> RemoveUserAsync(int departmentId, int usrId) =>
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
