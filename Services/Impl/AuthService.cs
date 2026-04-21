using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CrmWebApi.Common;
using CrmWebApi.Data.Entities;
using CrmWebApi.DTOs.Auth;
using CrmWebApi.DTOs.User;
using CrmWebApi.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CrmWebApi.Services.Impl;

public class AuthService(
	IUserRepository userRepo,
	IRefreshRepository refreshRepo,
	IEmailTokenRepository emailTokenRepo,
	IEmailService emailService,
	IConfiguration config,
	ILogger<AuthService> logger
) : IAuthService
{
	private const int TokenTypeConfirmation = 0;
	private const int TokenTypePasswordReset = 1;

	public async Task<Result<PendingConfirmationResponse>> RegisterAsync(RegisterRequest req)
	{
		if (await userRepo.ExistsAsync(u => u.UsrLogin == req.Login || u.UsrEmail == req.Email))
			return Error.Conflict("Пользователь с такими данными уже зарегистрирован");

		var requireEmailConfirmation = IsEmailConfirmationRequired();
		var user = new Usr
		{
			UsrFirstname = req.FirstName,
			UsrLastname = req.LastName,
			UsrEmail = req.Email,
			UsrLogin = req.Login,
			UsrPasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
			IsEmailConfirmed = !requireEmailConfirmation,
		};

		await userRepo.AddAsync(user);

		if (!requireEmailConfirmation)
			return new PendingConfirmationResponse(req.Email!, EmailConfirmationRequired: false);

		try
		{
			var code = await SendOtpAsync(user, TokenTypeConfirmation, expiryHours: 24);
			var displayName = BuildDisplayName(req.FirstName, req.LastName, req.Login);
			await emailService.SendEmailConfirmationAsync(req.Email!, displayName, code);
			return new PendingConfirmationResponse(req.Email!, EmailConfirmationRequired: true);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to send confirmation email to {Email}", req.Email);
			await emailTokenRepo.DeleteAllForUserAsync(user.UsrId, TokenTypeConfirmation);
			user.IsDeleted = true;
			await userRepo.UpdateAsync(user);
			await refreshRepo.RevokeAllForUserAsync(user.UsrId);
			return Error.Failure("Не удалось отправить письмо. Попробуйте позже.");
		}
	}

	public async Task<Result<AuthResponse>> ConfirmEmailAsync(ConfirmEmailRequest req)
	{
		var user = await userRepo.QueryLite().FirstOrDefaultAsync(u => u.UsrEmail == req.Email && !u.IsDeleted);
		if (user is null || user.IsEmailConfirmed)
			return Error.Validation("Неверный или истёкший код");

		var otpResult = await VerifyOtpAsync(user.UsrId, req.Code, TokenTypeConfirmation);
		if (!otpResult.IsSuccess)
			return Error.Validation(otpResult.Error!.Message);

		user.IsEmailConfirmed = true;
		await userRepo.UpdateAsync(user);

		return await IssueTokensAsync(user);
	}

	public async Task<Result> ResendConfirmationAsync(string email)
	{
		if (!IsEmailConfirmationRequired())
			return Result.Success();

		var user = await userRepo.QueryLite().FirstOrDefaultAsync(u => u.UsrEmail == email && !u.IsDeleted);
		if (user is null || user.IsEmailConfirmed)
			return Result.Success();

		var existing = await emailTokenRepo.GetActiveByUserAndTypeAsync(
			user.UsrId,
			TokenTypeConfirmation
		);

		// INFO: Frontend should create cooldown logic
		if (existing is not null)
			return Result.Success();

		var code = await SendOtpAsync(user, TokenTypeConfirmation, expiryHours: 24);

		var name = BuildDisplayName(user.UsrFirstname, user.UsrLastname, user.UsrLogin);
		try
		{
			await emailService.SendEmailConfirmationAsync(email, name, code);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to send confirmation email to {Email}", email);
			return Error.Failure("Не удалось отправить письмо. Попробуйте позже.");
		}
		return Result.Success();
	}

	public async Task<Result<AuthResponse>> LoginAsync(LoginRequest req)
	{
		var user = await userRepo
			.QueryLite()
			.FirstOrDefaultAsync(u => u.UsrLogin == req.Login && !u.IsDeleted);

		if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.UsrPasswordHash))
			return Error.Unauthorized("Неверный логин или пароль");

		if (IsEmailConfirmationRequired() && !user.IsEmailConfirmed)
			return Error.Forbidden(
				"Email не подтверждён",
				new Dictionary<string, object?> { ["email"] = user.UsrEmail }
			);

		return await IssueTokensAsync(user);
	}

	public async Task<Result<AuthResponse>> RefreshAsync(string refreshToken)
	{
		var hash = HashToken(refreshToken);
		var stored = await refreshRepo.GetByTokenHashAsync(hash);
		
		if (stored is null) {
			logger.LogWarning("Invalid refresh token: {Hash}", hash);
			return Error.Unauthorized("Refresh token не найден или уже использован");
		}

		if (stored.RefreshExpiresAt < DateTime.UtcNow)
		{
			await refreshRepo.DeleteAsync(stored);
			return Error.Unauthorized("Refresh token истёк");
		}

		await refreshRepo.DeleteAsync(stored);

		var user = await userRepo.QueryLite().FirstOrDefaultAsync(u => u.UsrId == stored.UsrId);
		if (user is null)
			return Error.Unauthorized("Пользователь не найден или удалён");

		return await IssueTokensAsync(user);
	}

	public async Task<Result> LogoutAsync(string refreshToken, int currentUserId)
	{
		var hash = HashToken(refreshToken);
		var stored = await refreshRepo.GetByTokenHashAsync(hash);
		if (stored is not null && stored.UsrId == currentUserId)
			await refreshRepo.DeleteAsync(stored);
		return Result.Success();
	}

	public async Task<Result> ForgotPasswordAsync(string email)
	{
		var user = await userRepo
			.QueryLite()
			.FirstOrDefaultAsync(u => u.UsrEmail == email && u.IsEmailConfirmed && !u.IsDeleted);
		if (user is null)
			return Result.Success();

		var existing = await emailTokenRepo.GetActiveByUserAndTypeAsync(
			user.UsrId,
			TokenTypePasswordReset
		);

		// INFO: Frontend should create cooldown logic
		if (existing is not null)
			return Result.Success();

		var code = await SendOtpAsync(user, TokenTypePasswordReset, expiryHours: 1);

		var name = BuildDisplayName(user.UsrFirstname, user.UsrLastname, user.UsrLogin);
		try
		{
			await emailService.SendPasswordResetAsync(email, name, code);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to send password reset email to {Email}", email);
			return Error.Failure("Не удалось отправить письмо. Попробуйте позже.");
		}
		return Result.Success();
	}

	public async Task<Result> ResetPasswordAsync(ResetPasswordRequest req)
	{
		var user = await userRepo.QueryLite().FirstOrDefaultAsync(u => u.UsrEmail == req.Email && !u.IsDeleted);
		if (user is null)
			return Error.Validation("Такой email не зарегистрирован");

		var otpResult = await VerifyOtpAsync(user.UsrId, req.Code, TokenTypePasswordReset);
		if (!otpResult.IsSuccess)
			return otpResult;

		user.UsrPasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
		await userRepo.UpdateAsync(user);

		await refreshRepo.RevokeAllForUserAsync(user.UsrId);
		return Result.Success();
	}

	private async Task<string> SendOtpAsync(Usr user, int tokenType, int expiryHours)
	{
		var codeBytes = RandomNumberGenerator.GetBytes(4);
		var code = (BitConverter.ToUInt32(codeBytes) % 900_000 + 100_000).ToString();
		var stored = new EmailToken
		{
			UsrId = user.UsrId,
			TokenHash = HashToken(code),
			TokenType = tokenType,
			ExpiresAt = DateTime.UtcNow.AddHours(expiryHours),
		};
		await emailTokenRepo.AddAsync(stored);
		return code;
	}

	private const int MaxOtpAttempts = 5;

	private async Task<Result> VerifyOtpAsync(int usrId, string code, int tokenType)
	{
		var token = await emailTokenRepo.GetActiveByUserAndTypeAsync(usrId, tokenType);
		if (token is null)
			return Error.Validation("Токен не найден или истёк");

		token.AttemptCount++;

		if (token.AttemptCount >= MaxOtpAttempts)
		{
			await emailTokenRepo.DeleteAllForUserAsync(usrId, tokenType);
			return Error.Validation("Слишком много попыток. Запросите новый код.");
		}

		if (token.TokenHash != HashToken(code.Trim()))
		{
			await emailTokenRepo.UpdateAsync(token);
			return Error.Validation("Неверный или истёкший код");
		}

		await emailTokenRepo.DeleteAllForUserAsync(usrId, tokenType);

		return Result.Success();
	}

	// WARNING: Not have validation for user existence, should be done before calling this method
	private async Task<AuthResponse> IssueTokensAsync(Usr user)
	{
		var fullUser = await userRepo.QueryHard().FirstAsync(u => u.UsrId == user.UsrId);
		var accessToken = GenerateAccessToken(fullUser);
		var (raw, stored) = GenerateRefreshToken(fullUser.UsrId);
		await refreshRepo.AddAsync(stored);
		return new AuthResponse(accessToken, raw, UserResponse.From(fullUser));
	}

	private string GenerateAccessToken(Usr user)
	{
		var secret = config["Jwt:Secret"]!;
		var issuer = config["Jwt:Issuer"]!;
		var audience = config["Jwt:Audience"]!;
		var ttl = int.Parse(config["Jwt:AccessTokenTtlMinutes"] ?? "15");

		var claims = new List<Claim>
		{
			new(ClaimTypes.NameIdentifier, user.UsrId.ToString()),
			new(ClaimTypes.Name, user.UsrLogin),
		};

		claims.AddRange(
			user.UsrPolicies.Select(p => new Claim(ClaimTypes.Role, p.Policy.PolicyName))
		);

		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
		var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
		var token = new JwtSecurityToken(
			issuer,
			audience,
			claims,
			expires: DateTime.UtcNow.AddMinutes(ttl),
			signingCredentials: creds
		);

		return new JwtSecurityTokenHandler().WriteToken(token);
	}

	private (string raw, Refresh stored) GenerateRefreshToken(int usrId)
	{
		var ttlDays = int.Parse(config["Jwt:RefreshTokenTtlDays"] ?? "7");
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

	private bool IsEmailConfirmationRequired() =>
		!bool.TryParse(config["Auth:RequireEmailConfirmation"], out var required) || required;

	private static string BuildDisplayName(string? first, string? last, string fallback)
	{
		var name = $"{first ?? ""} {last ?? ""}".Trim();
		return name == "" ? fallback : name;
	}
}
