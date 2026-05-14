using CrmWebApi.Common;
using CrmWebApi.Data.Entities;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Auth;
using CrmWebApi.DTOs.Policy;
using CrmWebApi.DTOs.User;
using CrmWebApi.Repositories;
using CrmWebApi.Services;
using CrmWebApi.Services.Impl;
using Microsoft.Extensions.Caching.Hybrid;

namespace CrmWebApi.Tests;

public sealed class UserServiceTests
{
	private static UserService CreateService(InMemoryUserRepository repo) =>
		new(
			repo,
			new NoopSessionService(),
			new PasswordHasher(),
			new NoopHybridCache(),
			new FixedCurrentUserService(Scope.ForAll(1))
		);

	[Fact]
	public async Task GetByIdAsync_ReturnsNotFound_WhenUserMissing()
	{
		var service = CreateService(new InMemoryUserRepository([]));

		var result = await service.GetByIdAsync(999);

		Assert.False(result.IsSuccess);
		Assert.Equal(ErrorType.NotFound, result.Error!.Type);
	}

	[Fact]
	public async Task GetByIdAsync_ReturnsUser_WhenExists()
	{
		var user = MakeUser(1, "john");
		var service = CreateService(new InMemoryUserRepository([user]));

		var result = await service.GetByIdAsync(1);

		Assert.True(result.IsSuccess);
		Assert.Equal("john", result.Value!.Login);
	}

	[Fact]
	public async Task CreateAsync_ReturnsConflict_WhenLoginAlreadyExists()
	{
		var existing = MakeUser(1, "john");
		var service = CreateService(new InMemoryUserRepository([existing]));

		var result = await service.CreateAsync(
			new CreateUserRequest("John", "Doe", "j@example.com", "john", "password1", [])
		);

		Assert.False(result.IsSuccess);
		Assert.Equal(ErrorType.Conflict, result.Error!.Type);
	}

	[Fact]
	public async Task CreateAsync_MarksAdminCreatedUserAsEmailConfirmed()
	{
		var repo = new InMemoryUserRepository([]);
		var service = CreateService(repo);

		var result = await service.CreateAsync(
			new CreateUserRequest("Jane", "Doe", "jane@example.com", "jane", "password1", [])
		);

		Assert.True(result.IsSuccess);
		Assert.True(repo.Users.Single().IsEmailConfirmed);
	}

	[Fact]
	public async Task ChangePasswordAsync_ReturnsNotFound_WhenUserMissing()
	{
		var service = CreateService(new InMemoryUserRepository([]));

		var result = await service.ChangePasswordAsync(999, new ChangePasswordRequest("old", "new1234"));

		Assert.False(result.IsSuccess);
		Assert.Equal(ErrorType.NotFound, result.Error!.Type);
	}

	[Fact]
	public async Task ChangePasswordAsync_ReturnsUnauthorized_WhenPasswordWrong()
	{
		var hasher = new PasswordHasher();
		var user = MakeUser(1, "john", hasher.Hash("correct123"));
		var service = CreateService(new InMemoryUserRepository([user]));

		var result = await service.ChangePasswordAsync(1, new ChangePasswordRequest("wrong", "new1234"));

		Assert.False(result.IsSuccess);
		Assert.Equal(ErrorType.Unauthorized, result.Error!.Type);
	}

	[Fact]
	public async Task ChangePasswordAsync_Succeeds_AndUpdatesHash()
	{
		var hasher = new PasswordHasher();
		var user = MakeUser(1, "john", hasher.Hash("correct123"));
		var service = CreateService(new InMemoryUserRepository([user]));

		var result = await service.ChangePasswordAsync(1, new ChangePasswordRequest("correct123", "newPass1"));

		Assert.True(result.IsSuccess);
		Assert.True(hasher.Verify("newPass1", user.UsrPasswordHash));
	}

	private static Usr MakeUser(int id, string login, string? passwordHash = null) =>
		new()
		{
			UsrId = id,
			UsrFirstname = "Test",
			UsrLastname = "User",
			UsrEmail = $"{login}@example.com",
			UsrLogin = login,
			UsrPasswordHash = passwordHash ?? "hash",
			IsEmailConfirmed = true,
		};

	private sealed class InMemoryUserRepository(IEnumerable<Usr> users) : IUserRepository
	{
		private readonly List<Usr> _users = users.ToList();

		public IReadOnlyList<Usr> Users => _users;

		public Task<PagedResponse<UserResponse>> GetPagedForScopeAsync(
			int page,
			int pageSize,
			Scope scope,
			bool includeTotal
		)
		{
			var users = _users.Where(u => !u.IsDeleted).Select(UserResponse.From).ToList();
			return Task.FromResult(new PagedResponse<UserResponse>(
				users.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
				page,
				pageSize,
				includeTotal ? users.Count : 0
			));
		}

		public Task<Usr?> GetByIdWithPoliciesAsync(int id) =>
			Task.FromResult(_users.FirstOrDefault(u => !u.IsDeleted && u.UsrId == id));

		public Task<Usr?> GetByIdForUpdateAsync(int id) =>
			Task.FromResult(_users.FirstOrDefault(u => !u.IsDeleted && u.UsrId == id));

		public Task<Usr?> GetByLoginWithPoliciesAsync(string loginLower) =>
			Task.FromResult(_users.FirstOrDefault(u => !u.IsDeleted && u.UsrLogin.ToLower() == loginLower));

		public Task<Usr?> GetByEmailForUpdateAsync(string emailLower) =>
			Task.FromResult(_users.FirstOrDefault(u => !u.IsDeleted && u.UsrEmail.ToLower() == emailLower));

		public Task<Usr?> GetConfirmedByEmailAsync(string emailLower) =>
			Task.FromResult(_users.FirstOrDefault(u => !u.IsDeleted && u.IsEmailConfirmed && u.UsrEmail.ToLower() == emailLower));

		public Task<bool> ExistsActiveByLoginOrEmailAsync(string loginLower, string emailLower) =>
			Task.FromResult(_users.Any(u => !u.IsDeleted && (u.UsrLogin.ToLower() == loginLower || u.UsrEmail.ToLower() == emailLower)));

		public Task<bool> ExistsActiveLoginAsync(string loginLower) =>
			Task.FromResult(_users.Any(u => !u.IsDeleted && u.UsrLogin.ToLower() == loginLower));

		public Task<Usr> AddAsync(Usr entity)
		{
			_users.Add(entity);
			return Task.FromResult(entity);
		}

		public Task<Usr> AddWithPoliciesAsync(Usr entity, IEnumerable<int> policyIds)
		{
			_users.Add(entity);
			return Task.FromResult(entity);
		}

		public Task UpdateAsync(Usr entity) => Task.CompletedTask;

		public Task<int> UpdateNamesAsync(int id, string? firstName, string? lastName)
		{
			var user = _users.FirstOrDefault(u => !u.IsDeleted && u.UsrId == id);
			if (user is null) return Task.FromResult(0);
			user.UsrFirstname = firstName ?? user.UsrFirstname;
			user.UsrLastname = lastName ?? user.UsrLastname;
			return Task.FromResult(1);
		}

		public Task<int> SoftDeleteAsync(int id)
		{
			var user = _users.FirstOrDefault(u => !u.IsDeleted && u.UsrId == id);
			if (user is null) return Task.FromResult(0);
			user.IsDeleted = true;
			return Task.FromResult(1);
		}

		public Task LinkPolicyAsync(int userId, int policyId) => Task.CompletedTask;

		public Task UnlinkPolicyAsync(int userId, int policyId) => Task.CompletedTask;

		public Task<IReadOnlyList<PolicyResponse>> GetPoliciesAsync(CancellationToken ct = default) =>
			Task.FromResult<IReadOnlyList<PolicyResponse>>([]);

		public Task<PolicyResponse?> GetPolicyByIdAsync(int id) =>
			Task.FromResult<PolicyResponse?>(null);
	}

	private sealed class NoopSessionService : IAuthSessionService
	{
		public Task<Result<AuthTokens>> IssueAsync(int usrId) =>
			Task.FromResult<Result<AuthTokens>>(
				new AuthTokens(
					"access",
					"refresh",
					new UserResponse(usrId, "Test", "User", "test@example.com", "test", [])
				)
			);

		public Task<Result<AuthTokens>> RefreshAsync(string refreshToken) =>
			Task.FromResult<Result<AuthTokens>>(Error.Unauthorized("noop"));

		public Task<Result> LogoutAsync(string refreshToken) =>
			Task.FromResult(Result.Success());

		public Task RevokeAllForUserAsync(int usrId) => Task.CompletedTask;
	}

	private sealed class FixedCurrentUserService(Scope? scope) : ICurrentUserService
	{
		public int? UsrId => scope?.CurrentUsrId;

		public Scope? Scope => scope;
	}

	private sealed class NoopHybridCache : HybridCache
	{
		public override ValueTask<T> GetOrCreateAsync<TState, T>(
			string key,
			TState state,
			Func<TState, CancellationToken, ValueTask<T>> factory,
			HybridCacheEntryOptions? options = null,
			IEnumerable<string>? tags = null,
			CancellationToken cancellationToken = default
		) => factory(state, cancellationToken);

		public override ValueTask SetAsync<T>(
			string key,
			T value,
			HybridCacheEntryOptions? options = null,
			IEnumerable<string>? tags = null,
			CancellationToken cancellationToken = default
		) => ValueTask.CompletedTask;

		public override ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default) =>
			ValueTask.CompletedTask;

		public override ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default) =>
			ValueTask.CompletedTask;
	}
}
