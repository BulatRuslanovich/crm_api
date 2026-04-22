using CrmWebApi.Data.Entities;
using CrmWebApi.Options;
using CrmWebApi.Repositories;
using CrmWebApi.Services;
using CrmWebApi.Services.Impl;

namespace CrmWebApi.Tests;

public sealed class EmailOtpServiceTests
{
	[Fact]
	public async Task CreateAsync_ReturnsNull_WhenActiveTokenAlreadyExists()
	{
		var repo = new InMemoryEmailTokenRepository();
		var service = CreateService(repo);
		var user = new Usr { UsrId = 10 };

		var firstCode = await service.CreateAsync(
			user,
			EmailOtpPurpose.PasswordReset,
			TimeSpan.FromMinutes(5)
		);
		var secondCode = await service.CreateAsync(
			user,
			EmailOtpPurpose.PasswordReset,
			TimeSpan.FromMinutes(5)
		);

		Assert.NotNull(firstCode);
		Assert.Null(secondCode);
		Assert.Equal(1, repo.Count);
	}

	[Fact]
	public async Task VerifyAsync_DeletesTokenAfterSuccessfulVerification()
	{
		var repo = new InMemoryEmailTokenRepository();
		var service = CreateService(repo);
		var user = new Usr { UsrId = 11 };
		var code = await service.CreateAsync(
			user,
			EmailOtpPurpose.EmailConfirmation,
			TimeSpan.FromMinutes(5)
		);

		var result = await service.VerifyAsync(
			user.UsrId,
			code!,
			EmailOtpPurpose.EmailConfirmation
		);

		Assert.True(result.IsSuccess);
		Assert.Equal(0, repo.Count);
	}

	[Fact]
	public async Task VerifyAsync_DeletesTokenAfterMaxAttempts()
	{
		var repo = new InMemoryEmailTokenRepository();
		var service = CreateService(repo);
		var user = new Usr { UsrId = 12 };
		await service.CreateAsync(user, EmailOtpPurpose.PasswordReset, TimeSpan.FromMinutes(5));

		for (var attempt = 0; attempt < 4; attempt++)
		{
			var retryResult = await service.VerifyAsync(
				user.UsrId,
				"000000",
				EmailOtpPurpose.PasswordReset
			);
			Assert.False(retryResult.IsSuccess);
			Assert.Equal(1, repo.Count);
		}

		var finalResult = await service.VerifyAsync(
			user.UsrId,
			"000000",
			EmailOtpPurpose.PasswordReset
		);

		Assert.False(finalResult.IsSuccess);
		Assert.Equal(0, repo.Count);
	}

	private static EmailOtpService CreateService(InMemoryEmailTokenRepository repo) =>
		new(
			repo,
			Microsoft.Extensions.Options.Options.Create(
				new JwtOptions
				{
					Secret = "test-secret-with-more-than-32-characters",
					Issuer = "issuer",
					Audience = "audience",
				}
			),
			Microsoft.Extensions.Options.Options.Create(
				new AuthOptions { OtpHashSecret = "otp-secret-with-more-than-32-characters" }
			)
		);

	private sealed class InMemoryEmailTokenRepository : IEmailTokenRepository
	{
		private readonly List<EmailToken> _tokens = [];

		public int Count => _tokens.Count;

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
}
