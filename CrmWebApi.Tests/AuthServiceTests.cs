using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CrmWebApi.Common;
using CrmWebApi.Data.Entities;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Auth;
using CrmWebApi.DTOs.Policy;
using CrmWebApi.DTOs.User;
using CrmWebApi.Options;
using CrmWebApi.Repositories;
using CrmWebApi.Services;
using CrmWebApi.Services.Impl;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging.Abstractions;

namespace CrmWebApi.Tests;

public class AuthServiceTests
{
	[Fact]
	public async Task RefreshAsync_ConsumesRefreshTokenOnlyOnce()
	{
		// Arrange: create one valid refresh token stored by hash, as production does.
		const string rawRefreshToken = "raw-refresh-token";
		var user = TestUsers.UserWithRole(1, "Admin");
		var refreshRepo = new InMemoryRefreshRepository(
			new Refresh
			{
				UsrId = user.UsrId,
				RefreshTokenHash = Sha256(rawRefreshToken),
				RefreshExpiresAt = DateTime.UtcNow.AddMinutes(5),
			}
		);
		var service = CreateService([user], refreshRepo);

		// Act: try to refresh twice with the same raw token.
		var first = await service.RefreshAsync(rawRefreshToken);
		var second = await service.RefreshAsync(rawRefreshToken);

		// Assert: the first call consumes the token, the second call is rejected.
		Assert.True(first.IsSuccess);
		Assert.NotNull(first.Value?.RefreshToken);
		Assert.False(second.IsSuccess);
	}

	[Fact]
	public async Task IssueAsync_LoadsPoliciesForAccessToken()
	{
		var user = TestUsers.UserWithRole(1, RoleNames.Admin);
		var service = CreateSessionService([user], new InMemoryRefreshRepository());

		var result = await service.IssueAsync(user.UsrId);

		Assert.True(result.IsSuccess);
		var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Value!.AccessToken);
		Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Role && c.Value == RoleNames.Admin);
	}

	[Fact]
	public async Task ResetPasswordAsync_ReturnsSuccessForUnknownEmail()
	{
		// Arrange: no user exists with the requested email.
		var service = CreateService([], new InMemoryRefreshRepository());

		// Act: request password reset for an unknown email.
		var result = await service.ResetPasswordAsync(
			new ResetPasswordRequest("missing@example.com", "123456", "newPassword1")
		);

		// Assert: the service does not leak whether an email is registered.
		Assert.True(result.IsSuccess);
	}

	private static AuthService CreateService(
		IEnumerable<Usr> users,
		IRefreshRepository refreshRepository
	)
	{
		var userRepository = new InMemoryUserRepository(users);
		var jwtOptions = Microsoft.Extensions.Options.Options.Create(
			new JwtOptions
			{
				Secret = "test-secret-with-more-than-32-characters",
				Issuer = "issuer",
				Audience = "audience",
			}
		);
		var authOptions = Microsoft.Extensions.Options.Options.Create(
			new AuthOptions { OtpHashSecret = "otp-secret-with-more-than-32-characters" }
		);
		var sessionService = CreateSessionService(userRepository, refreshRepository, jwtOptions);
		var emailOtpService = new EmailOtpService(
			new InMemoryEmailTokenRepository(),
			jwtOptions,
			authOptions
		);

		return new AuthService(
			userRepository,
			emailOtpService,
			new NoopEmailService(),
			sessionService,
			new PasswordHasher(),
			authOptions,
			NullLogger<AuthService>.Instance
		);
	}

	private static AuthSessionService CreateSessionService(
		IEnumerable<Usr> users,
		IRefreshRepository refreshRepository
	) =>
		CreateSessionService(
			new InMemoryUserRepository(users),
			refreshRepository,
			Microsoft.Extensions.Options.Options.Create(
				new JwtOptions
				{
					Secret = "test-secret-with-more-than-32-characters",
					Issuer = "issuer",
					Audience = "audience",
				}
			)
		);

	private static AuthSessionService CreateSessionService(
		IUserRepository userRepository,
		IRefreshRepository refreshRepository,
		Microsoft.Extensions.Options.IOptions<JwtOptions> jwtOptions
	) =>
		new(
			userRepository,
			refreshRepository,
			jwtOptions,
			NullLogger<AuthSessionService>.Instance
		);

	private static string Sha256(string value)
	{
		var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
		return Convert.ToHexString(bytes).ToLowerInvariant();
	}

	private sealed class InMemoryUserRepository(IEnumerable<Usr> users) : IUserRepository
	{
		private readonly List<Usr> _users = users.ToList();

		public Task<PagedResponse<UserResponse>> GetPagedForScopeAsync(
			int page,
			int pageSize,
			Scope scope,
			bool includeTotal
		) =>
			Task.FromResult(new PagedResponse<UserResponse>(
				_users.Where(u => !u.IsDeleted).Select(UserResponse.From).ToList(),
				page,
				pageSize,
				includeTotal ? _users.Count(u => !u.IsDeleted) : 0
			));

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

	private sealed class InMemoryRefreshRepository(params Refresh[] refreshes) : IRefreshRepository
	{
		private readonly List<Refresh> _refreshes = refreshes.ToList();

		public Task<Refresh> AddAsync(Refresh entity)
		{
			_refreshes.Add(entity);
			return Task.FromResult(entity);
		}

		public Task DeleteByHashAsync(string tokenHash)
		{
			_refreshes.RemoveAll(r => r.RefreshTokenHash == tokenHash);
			return Task.CompletedTask;
		}

		public Task<Refresh?> GetByTokenHashAsync(string tokenHash) =>
			Task.FromResult(_refreshes.FirstOrDefault(r => r.RefreshTokenHash == tokenHash));

		public Task<Refresh?> ConsumeByTokenHashAsync(string tokenHash)
		{
			var refresh = _refreshes.FirstOrDefault(r => r.RefreshTokenHash == tokenHash);
			if (refresh is not null)
				_refreshes.Remove(refresh);
			return Task.FromResult(refresh);
		}

		public Task RevokeAllForUserAsync(int usrId)
		{
			_refreshes.RemoveAll(r => r.UsrId == usrId);
			return Task.CompletedTask;
		}
	}

	private sealed class InMemoryEmailTokenRepository : IEmailTokenRepository
	{
		private readonly List<EmailToken> _tokens = [];

		public Task<EmailToken?> CreateIfNoActiveAsync(EmailToken entity)
		{
			if (
				_tokens.Any(t =>
					t.UsrId == entity.UsrId
					&& t.TokenType == entity.TokenType
					&& t.ExpiresAt > DateTime.UtcNow
				)
			)
				return Task.FromResult<EmailToken?>(null);

			_tokens.RemoveAll(t => t.UsrId == entity.UsrId && t.TokenType == entity.TokenType);
			_tokens.Add(entity);
			return Task.FromResult<EmailToken?>(entity);
		}

		public Task UpdateAsync(EmailToken entity) => Task.CompletedTask;

		public Task<EmailToken?> GetActiveByUserAndTypeAsync(int usrId, int tokenType) =>
			Task.FromResult(
				_tokens.FirstOrDefault(t =>
					t.UsrId == usrId && t.TokenType == tokenType && t.ExpiresAt > DateTime.UtcNow
				)
			);

		public Task DeleteAllForUserAsync(int usrId, int tokenType)
		{
			_tokens.RemoveAll(t => t.UsrId == usrId && t.TokenType == tokenType);
			return Task.CompletedTask;
		}
	}

	private sealed class NoopEmailService : IEmailService
	{
		public Task SendEmailConfirmationAsync(string toEmail, string toName, string code) =>
			Task.CompletedTask;

		public Task SendPasswordResetAsync(string toEmail, string toName, string code) =>
			Task.CompletedTask;
	}

	private static IQueryable<T> AsAsyncQueryable<T>(IEnumerable<T> source) =>
		new TestAsyncEnumerable<T>(source);

	private sealed class TestAsyncQueryProvider<TEntity>(IQueryProvider inner) : IAsyncQueryProvider
	{
		public IQueryable CreateQuery(Expression expression) =>
			new TestAsyncEnumerable<TEntity>(expression);

		public IQueryable<TElement> CreateQuery<TElement>(Expression expression) =>
			new TestAsyncEnumerable<TElement>(expression);

		public object? Execute(Expression expression) => inner.Execute(expression);

		public TResult Execute<TResult>(Expression expression) => inner.Execute<TResult>(expression);

		public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
		{
			var resultType = typeof(TResult).GetGenericArguments()[0];
			var result = typeof(IQueryProvider)
				.GetMethod(nameof(IQueryProvider.Execute), 1, [typeof(Expression)])!
				.MakeGenericMethod(resultType)
				.Invoke(this, [expression]);

			return (TResult)
				typeof(Task)
					.GetMethod(nameof(Task.FromResult))!
					.MakeGenericMethod(resultType)
					.Invoke(null, [result])!;
		}
	}

	private sealed class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
	{
		public TestAsyncEnumerable(IEnumerable<T> enumerable)
			: base(enumerable) { }

		public TestAsyncEnumerable(Expression expression)
			: base(expression) { }

		public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
			new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());

		IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
	}

	private sealed class TestAsyncEnumerator<T>(IEnumerator<T> inner) : IAsyncEnumerator<T>
	{
		public T Current => inner.Current;

		public ValueTask DisposeAsync()
		{
			inner.Dispose();
			return ValueTask.CompletedTask;
		}

		public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(inner.MoveNext());
	}
}
