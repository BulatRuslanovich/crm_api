using CrmWebApi.Common;
using CrmWebApi.Data.Entities;
using CrmWebApi.DTOs.Auth;
using CrmWebApi.Options;
using CrmWebApi.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CrmWebApi.Services.Impl;

public class AuthService(
	IUserRepository userRepo,
	IEmailOtpService emailOtpService,
	IEmailService emailService,
	IAuthSessionService sessionService,
	IPasswordHasher passwordHasher,
	IOptions<AuthOptions> authOptions,
	ILogger<AuthService> logger
) : IAuthService
{
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
			UsrPasswordHash = passwordHasher.Hash(req.Password),
			IsEmailConfirmed = !requireEmailConfirmation,
		};

		await userRepo.AddAsync(user);

		if (!requireEmailConfirmation)
			return new PendingConfirmationResponse(req.Email, EmailConfirmationRequired: false);

		try
		{
			var code = await emailOtpService.CreateAsync(
				user,
				EmailOtpPurpose.EmailConfirmation,
				TimeSpan.FromHours(24)
			);
			if (code is null)
				throw new InvalidOperationException("Active confirmation OTP already exists for new user");

			var displayName = BuildDisplayName(req.FirstName, req.LastName, req.Login);
			await emailService.SendEmailConfirmationAsync(req.Email, displayName, code);
			return new PendingConfirmationResponse(req.Email, EmailConfirmationRequired: true);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to send confirmation email to {Email}", req.Email);
			await emailOtpService.DeleteAllAsync(user.UsrId, EmailOtpPurpose.EmailConfirmation);
			user.IsDeleted = true;
			await userRepo.UpdateAsync(user);
			await sessionService.RevokeAllForUserAsync(user.UsrId);
			return Error.Failure("Не удалось отправить письмо. Попробуйте позже.");
		}
	}

	public async Task<Result<AuthTokens>> ConfirmEmailAsync(ConfirmEmailRequest req)
	{
		var user = await userRepo.QueryForUpdate().FirstOrDefaultAsync(u => u.UsrEmail == req.Email);
		if (user is null || user.IsEmailConfirmed)
			return Error.Validation("Неверный или истёкший код");

		var otpResult = await emailOtpService.VerifyAsync(
			user.UsrId,
			req.Code,
			EmailOtpPurpose.EmailConfirmation
		);
		if (!otpResult.IsSuccess)
			return Error.Validation(otpResult.Error!.Message);

		user.IsEmailConfirmed = true;
		await userRepo.UpdateAsync(user);

		return await sessionService.IssueAsync(user);
	}

	public async Task<Result> ResendConfirmationAsync(string email)
	{
		if (!IsEmailConfirmationRequired())
			return Result.Success();

		var user = await userRepo.QueryLite().FirstOrDefaultAsync(u => u.UsrEmail == email && !u.IsDeleted);
		if (user is null || user.IsEmailConfirmed)
			return Result.Success();

		var code = await emailOtpService.CreateAsync(
			user,
			EmailOtpPurpose.EmailConfirmation,
			TimeSpan.FromHours(24)
		);
		if (code is null)
			return Result.Success();

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

	public async Task<Result<AuthTokens>> LoginAsync(LoginRequest req)
	{
		var user = await userRepo
			.QueryLite()
			.FirstOrDefaultAsync(u => u.UsrLogin == req.Login && !u.IsDeleted);

		if (user is null || !passwordHasher.Verify(req.Password, user.UsrPasswordHash))
			return Error.Unauthorized("Неверный логин или пароль");

		if (IsEmailConfirmationRequired() && !user.IsEmailConfirmed)
			return Error.Forbidden(
				"Email не подтверждён",
				new Dictionary<string, object?> { ["email"] = user.UsrEmail }
			);

		return await sessionService.IssueAsync(user);
	}

	public Task<Result<AuthTokens>> RefreshAsync(string refreshToken) =>
		sessionService.RefreshAsync(refreshToken);

	public Task<Result> LogoutAsync(string refreshToken) => sessionService.LogoutAsync(refreshToken);

	public async Task<Result> ForgotPasswordAsync(string email)
	{
		var user = await userRepo
			.QueryLite()
			.FirstOrDefaultAsync(u => u.UsrEmail == email && u.IsEmailConfirmed && !u.IsDeleted);
		if (user is null)
			return Result.Success();

		var code = await emailOtpService.CreateAsync(
			user,
			EmailOtpPurpose.PasswordReset,
			TimeSpan.FromHours(1)
		);
		if (code is null)
			return Result.Success();

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
		var user = await userRepo.QueryForUpdate().FirstOrDefaultAsync(u => u.UsrEmail == req.Email);
		if (user is null)
			return Result.Success();

		var otpResult = await emailOtpService.VerifyAsync(
			user.UsrId,
			req.Code,
			EmailOtpPurpose.PasswordReset
		);
		if (!otpResult.IsSuccess)
			return otpResult;

		user.UsrPasswordHash = passwordHasher.Hash(req.NewPassword);
		await userRepo.UpdateAsync(user);

		await sessionService.RevokeAllForUserAsync(user.UsrId);
		return Result.Success();
	}

	private bool IsEmailConfirmationRequired() =>
		authOptions.Value.RequireEmailConfirmation;

	private static string BuildDisplayName(string? first, string? last, string fallback)
	{
		var name = $"{first ?? ""} {last ?? ""}".Trim();
		return name == "" ? fallback : name;
	}
}
