using System.Security.Cryptography;
using System.Text;
using CrmWebApi.Common;
using CrmWebApi.Data.Entities;
using CrmWebApi.Options;
using CrmWebApi.Repositories;
using Microsoft.Extensions.Options;

namespace CrmWebApi.Services.Impl;

public sealed class EmailOtpService(
	IEmailTokenRepository emailTokenRepo,
	IOptions<JwtOptions> jwtOptions,
	IOptions<AuthOptions> authOptions
) : IEmailOtpService
{
	private const int MaxOtpAttempts = 5;

	public async Task<string?> CreateAsync(Usr user, EmailOtpPurpose purpose, TimeSpan ttl)
	{
		var codeBytes = RandomNumberGenerator.GetBytes(4);
		var code = (BitConverter.ToUInt32(codeBytes) % 900_000 + 100_000).ToString();
		var stored = new EmailToken
		{
			UsrId = user.UsrId,
			TokenHash = HashOtp(code),
			TokenType = (int)purpose,
			ExpiresAt = DateTime.UtcNow.Add(ttl),
		};

		var created = await emailTokenRepo.CreateIfNoActiveAsync(stored);
		return created is null ? null : code;
	}

	public async Task<Result> VerifyAsync(int usrId, string code, EmailOtpPurpose purpose)
	{
		var token = await emailTokenRepo.GetActiveByUserAndTypeAsync(usrId, (int)purpose);
		if (token is null)
			return Error.Validation("Токен не найден или истёк");

		token.AttemptCount++;

		if (token.AttemptCount >= MaxOtpAttempts)
		{
			await DeleteAllAsync(usrId, purpose);
			return Error.Validation("Слишком много попыток. Запросите новый код.");
		}

		if (!FixedTimeEquals(token.TokenHash, HashOtp(code)))
		{
			await emailTokenRepo.UpdateAsync(token);
			return Error.Validation("Неверный или истёкший код");
		}

		await DeleteAllAsync(usrId, purpose);
		return Result.Success();
	}

	public Task DeleteAllAsync(int usrId, EmailOtpPurpose purpose) =>
		emailTokenRepo.DeleteAllForUserAsync(usrId, (int)purpose);

	private string HashOtp(string code)
	{
		var secret = authOptions.Value.OtpHashSecret;
		if (string.IsNullOrWhiteSpace(secret))
			secret = jwtOptions.Value.Secret;

		var bytes = HMACSHA256.HashData(
			Encoding.UTF8.GetBytes(secret),
			Encoding.UTF8.GetBytes(code.Trim())
		);
		return Convert.ToHexString(bytes).ToLowerInvariant();
	}

	private static bool FixedTimeEquals(string left, string right) =>
		left.Length == right.Length
		&& CryptographicOperations.FixedTimeEquals(
			Encoding.UTF8.GetBytes(left),
			Encoding.UTF8.GetBytes(right)
		);
}
