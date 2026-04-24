using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using CrmWebApi.Common;
using CrmWebApi.Data.Entities;
using CrmWebApi.DTOs.Auth;
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
		var sessionService = new AuthSessionService(
			userRepository,
			refreshRepository,
			jwtOptions,
			NullLogger<AuthSessionService>.Instance
		);
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

	private static string Sha256(string value)
	{
		var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
		return Convert.ToHexString(bytes).ToLowerInvariant();
	}

	private sealed class InMemoryUserRepository(IEnumerable<Usr> users) : IUserRepository
	{
		private readonly List<Usr> _users = users.ToList();

		public IQueryable<Usr> QueryForScope(Scope scope) => QueryWithPolicies();

		public IQueryable<Usr> QueryWithPolicies() => AsAsyncQueryable(_users.Where(u => !u.IsDeleted));

		public IQueryable<Usr> QueryForRead() => AsAsyncQueryable(_users.Where(u => !u.IsDeleted));

		public IQueryable<Usr> QueryForUpdate() => AsAsyncQueryable(_users.Where(u => !u.IsDeleted));

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

		public IQueryable<Policy> QueryPolicies() => Enumerable.Empty<Policy>().AsQueryable();
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
