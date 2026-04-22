using CrmWebApi.Common;
using CrmWebApi.DTOs.Auth;
using CrmWebApi.Options;
using CrmWebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace CrmWebApi.Controllers;

[Route("api/auth")]
[EnableRateLimiting("auth")]
public class AuthController(
	IAuthService service,
	IOptions<JwtOptions> jwtOptions,
	IWebHostEnvironment environment
) : ApiController
{
	private readonly string _refreshTokenCookieName = environment.IsProduction() ? "__Host-refreshToken" : "refreshToken";

	[HttpPost("register")]
	[AllowAnonymous]
	[EndpointSummary("Register a new user")]
	[ProducesResponseType<PendingConfirmationResponse>(StatusCodes.Status202Accepted)]
	public async Task<IActionResult> Register([FromBody] RegisterRequest req)
	{
		var result = await service.RegisterAsync(req);
		if (!result.IsSuccess)
			return MapError(result.Error!);
		return Accepted(result.Value);
	}

	[HttpPost("confirm-email")]
	[AllowAnonymous]
	[EndpointSummary("Confirm email with OTP code")]
	[ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
	public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest req) =>
		AuthResult(await service.ConfirmEmailAsync(req));

	[HttpPost("resend-confirmation")]
	[AllowAnonymous]
	[EndpointSummary("Resend confirmation code")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	public async Task<IActionResult> ResendConfirmation([FromBody] string email) =>
		FromResult(await service.ResendConfirmationAsync(email));

	[HttpPost("forgot-password")]
	[AllowAnonymous]
	[EndpointSummary("Request password reset")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req) =>
		FromResult(await service.ForgotPasswordAsync(req.Email));

	[HttpPost("reset-password")]
	[AllowAnonymous]
	[EndpointSummary("Reset password with OTP code")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req) =>
		FromResult(await service.ResetPasswordAsync(req));

	[HttpPost("login")]
	[AllowAnonymous]
	[EndpointSummary("Login")]
	[ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
	public async Task<IActionResult> Login([FromBody] LoginRequest req) =>
		AuthResult(await service.LoginAsync(req));

	[HttpPost("refresh")]
	[AllowAnonymous]
	[EndpointSummary("Refresh tokens")]
	[ProducesResponseType<AccessTokenResponse>(StatusCodes.Status200OK)]
	public async Task<IActionResult> Refresh()
	{
		if (!Request.Cookies.TryGetValue(_refreshTokenCookieName, out var refreshToken))
			return MapError(Error.Unauthorized("Refresh token отсутствует"));

		var result = await service.RefreshAsync(refreshToken);
		if (!result.IsSuccess)
			return MapError(result.Error!);

		PutTokenToCookie(result.Value!.RefreshToken);
		return Ok(new AccessTokenResponse(result.Value.AccessToken));
	}

	[HttpPost("logout")]
	[AllowAnonymous]
	[EndpointSummary("Logout")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	public async Task<IActionResult> Logout()
	{
		if (Request.Cookies.TryGetValue(_refreshTokenCookieName, out var refreshToken))
		{
			var result = await service.LogoutAsync(refreshToken);
			if (!result.IsSuccess)
				return MapError(result.Error!);
		}

		Response.Cookies.Append(
			_refreshTokenCookieName,
			string.Empty,
			CreateCookieOptions(DateTimeOffset.UnixEpoch, maxAge: TimeSpan.Zero)
		);

		return NoContent();
	}

	private IActionResult AuthResult(Result<AuthTokens> result)
	{
		if (!result.IsSuccess)
			return MapError(result.Error!);

		PutTokenToCookie(result.Value!.RefreshToken);
		return Ok(new AuthResponse(result.Value.AccessToken, result.Value.User));
	}

	private void PutTokenToCookie(string refreshToken) =>
		Response.Cookies.Append(
			_refreshTokenCookieName,
			refreshToken,
			CreateCookieOptions(DateTimeOffset.UtcNow.AddDays(jwtOptions.Value.RefreshTokenTtlDays))
		);

	private CookieOptions CreateCookieOptions(
		DateTimeOffset expires,
		TimeSpan? maxAge = null
	) =>
		new()
		{
			HttpOnly = true,
			Secure = environment.IsProduction(),
			SameSite = SameSiteMode.Lax,
			Path = "/",
			Expires = expires,
			MaxAge = maxAge,
		};
}
