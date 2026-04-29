using System.Linq.Expressions;
using CrmWebApi.Common;
using CrmWebApi.Data.Entities;
using CrmWebApi.DTOs.Auth;
using CrmWebApi.DTOs.User;
using CrmWebApi.Repositories;
using CrmWebApi.Services;
using CrmWebApi.Services.Impl;
using Microsoft.Extensions.Caching.Hybrid;

namespace CrmWebApi.Tests;

public sealed class UserServiceTests
{
	private static UserService CreateService(InMemoryUserRepository repo) =>
		new(repo, new NoopSessionService(), new PasswordHasher(), new NoopHybridCache());

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

		public IQueryable<Usr> QueryForScope(Scope scope) => QueryWithPolicies();

		public IQueryable<Usr> QueryWithPolicies() =>
			_users.Where(u => !u.IsDeleted).AsAsyncQueryable();

		public IQueryable<Usr> QueryForRead() =>
			_users.Where(u => !u.IsDeleted).AsAsyncQueryable();

		public IQueryable<Usr> QueryForUpdate() =>
			_users.Where(u => !u.IsDeleted).AsAsyncQueryable();

		public Task<bool> ExistsAsync(Expression<Func<Usr, bool>> predicate) =>
			Task.FromResult(_users.AsQueryable().Any(predicate));

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

		public Task LinkPolicyAsync(int userId, int policyId) => Task.CompletedTask;

		public Task UnlinkPolicyAsync(int userId, int policyId) => Task.CompletedTask;

		public IQueryable<Policy> QueryPolicies() =>
			Enumerable.Empty<Policy>().AsAsyncQueryable();
	}

	private sealed class NoopSessionService : IAuthSessionService
	{
		public Task<AuthTokens> IssueAsync(Usr user) =>
			Task.FromResult(new AuthTokens("access", "refresh",
				new DTOs.User.UserResponse(user.UsrId, user.UsrFirstname, user.UsrLastname, user.UsrEmail, user.UsrLogin, [])));

		public Task<Result<AuthTokens>> RefreshAsync(string refreshToken) =>
			Task.FromResult<Result<AuthTokens>>(Error.Unauthorized("noop"));

		public Task<Result> LogoutAsync(string refreshToken) =>
			Task.FromResult(Result.Success());

		public Task RevokeAllForUserAsync(int usrId) => Task.CompletedTask;
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
