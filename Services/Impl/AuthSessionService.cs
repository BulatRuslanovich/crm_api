using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CrmWebApi.Common;
using CrmWebApi.Data.Entities;
using CrmWebApi.DTOs.Auth;
using CrmWebApi.DTOs.User;
using CrmWebApi.Options;
using CrmWebApi.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CrmWebApi.Services.Impl;

public sealed class AuthSessionService(
	IUserRepository userRepo,
	IRefreshRepository refreshRepo,
	IOptions<JwtOptions> jwtOptions,
	ILogger<AuthSessionService> logger
) : IAuthSessionService
{
	public async Task<AuthTokens> IssueAsync(Usr user)
	{
		var fullUser = await userRepo.QueryHard().FirstAsync(u => u.UsrId == user.UsrId);
		var accessToken = GenerateAccessToken(fullUser);
		var (raw, stored) = GenerateRefreshToken(fullUser.UsrId);
		await refreshRepo.AddAsync(stored);
		return new AuthTokens(accessToken, raw, UserResponse.From(fullUser));
	}

	public async Task<Result<AuthTokens>> RefreshAsync(string refreshToken)
	{
		var hash = HashToken(refreshToken);
		var stored = await refreshRepo.ConsumeByTokenHashAsync(hash);

		if (stored is null)
		{
			logger.LogWarning("Invalid or already consumed refresh token");
			return Error.Unauthorized("Refresh token не найден или уже использован");
		}

		if (stored.RefreshExpiresAt < DateTime.UtcNow)
			return Error.Unauthorized("Refresh token истёк");

		var user = await userRepo.QueryLite().FirstOrDefaultAsync(u => u.UsrId == stored.UsrId);
		if (user is null)
			return Error.Unauthorized("Пользователь не найден или удалён");

		return await IssueAsync(user);
	}

	public async Task<Result> LogoutAsync(string refreshToken)
	{
		var hash = HashToken(refreshToken);
		var stored = await refreshRepo.GetByTokenHashAsync(hash);
		if (stored is not null)
			await refreshRepo.DeleteAsync(stored);
		return Result.Success();
	}

	public Task RevokeAllForUserAsync(int usrId) => refreshRepo.RevokeAllForUserAsync(usrId);

	private string GenerateAccessToken(Usr user)
	{
		var jwt = jwtOptions.Value;

		var claims = new List<Claim>
		{
			new(ClaimTypes.NameIdentifier, user.UsrId.ToString()),
			new(ClaimTypes.Name, user.UsrLogin),
		};

		claims.AddRange(
			user.UsrPolicies.Select(p => new Claim(ClaimTypes.Role, p.Policy.PolicyName))
		);

		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret));
		var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
		var token = new JwtSecurityToken(
			jwt.Issuer,
			jwt.Audience,
			claims,
			expires: DateTime.UtcNow.AddMinutes(jwt.AccessTokenTtlMinutes),
			signingCredentials: creds
		);

		return new JwtSecurityTokenHandler().WriteToken(token);
	}

	private (string raw, Refresh stored) GenerateRefreshToken(int usrId)
	{
		var ttlDays = jwtOptions.Value.RefreshTokenTtlDays;
		var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
		var stored = new Refresh
		{
			UsrId = usrId,
			RefreshTokenHash = HashToken(raw),
			RefreshExpiresAt = DateTime.UtcNow.AddDays(ttlDays),
		};
		return (raw, stored);
	}

	private static string HashToken(string token)
	{
		var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
		return Convert.ToHexString(bytes).ToLowerInvariant();
	}
}
