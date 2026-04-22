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
[Tags("Auth")]
[EnableRateLimiting("auth")]
public class AuthController(
	IAuthService service,
	IOptions<JwtOptions> jwtOptions,
	IWebHostEnvironment environment
) : ApiController
{
	private readonly string RefreshTokenCookieName = environment.IsProduction() ? "__Host-refreshToken" : "refreshToken";

	[HttpPost("register")]
	[AllowAnonymous]
	[EndpointSummary("Register a new user")]
	[EndpointDescription(
		"Creates a user account. When Auth:RequireEmailConfirmation is enabled, sends a 6-digit OTP code and requires email confirmation before login."
	)]
	[ProducesResponseType<PendingConfirmationResponse>(StatusCodes.Status202Accepted)]
	[ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
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
	[EndpointDescription(
		"Verifies the 6-digit code sent during registration. Returns a JWT access token and sets the refresh token cookie."
	)]
	[ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
	[ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
	public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest req) =>
		AuthResult(await service.ConfirmEmailAsync(req));

	[HttpPost("resend-confirmation")]
	[AllowAnonymous]
	[EndpointSummary("Resend confirmation code")]
	[EndpointDescription(
		"Sends a new OTP code to the specified email. Returns 204 regardless of whether the email exists (prevents enumeration)."
	)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	public async Task<IActionResult> ResendConfirmation([FromBody] string email) =>
		FromResult(await service.ResendConfirmationAsync(email));

	[HttpPost("forgot-password")]
	[AllowAnonymous]
	[EndpointSummary("Request password reset")]
	[EndpointDescription(
		"Sends a 6-digit OTP code to the email for password reset. Silent response to prevent enumeration."
	)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req) =>
		FromResult(await service.ForgotPasswordAsync(req.Email));

	[HttpPost("reset-password")]
	[AllowAnonymous]
	[EndpointSummary("Reset password with OTP code")]
	[EndpointDescription(
		"Sets a new password after verifying the OTP code. All existing refresh tokens are revoked."
	)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
	public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req) =>
		FromResult(await service.ResetPasswordAsync(req));

	[HttpPost("login")]
	[AllowAnonymous]
	[EndpointSummary("Login")]
	[EndpointDescription(
		"Authenticates with login/password, returns a JWT access token and sets the refresh token cookie."
	)]
	[ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
	[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
	public async Task<IActionResult> Login([FromBody] LoginRequest req) =>
		AuthResult(await service.LoginAsync(req));

	[HttpPost("refresh")]
	[AllowAnonymous]
	[EndpointSummary("Refresh tokens")]
	[EndpointDescription(
		"Exchanges the refresh token cookie for a new access token and rotated refresh token cookie. The old refresh token is consumed."
	)]
	[ProducesResponseType<AccessTokenResponse>(StatusCodes.Status200OK)]
	[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
	public async Task<IActionResult> Refresh()
	{
		if (!Request.Cookies.TryGetValue(RefreshTokenCookieName, out var refreshToken))
			return MapError(Error.Unauthorized("Refresh token отсутствует"));

		var result = await service.RefreshAsync(refreshToken);
		if (!result.IsSuccess)
			return MapError(result.Error!);

		AppendRefreshTokenCookie(result.Value!.RefreshToken);
		return Ok(new AccessTokenResponse(result.Value.AccessToken));
	}

	[HttpPost("logout")]
	[AllowAnonymous]
	[EndpointSummary("Logout")]
	[EndpointDescription("Revokes the refresh token from cookie and clears the cookie.")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	public async Task<IActionResult> Logout()
	{
		if (Request.Cookies.TryGetValue(RefreshTokenCookieName, out var refreshToken))
		{
			var result = await service.LogoutAsync(refreshToken);
			if (!result.IsSuccess)
				return MapError(result.Error!);
		}

		DeleteRefreshTokenCookie();
		return NoContent();
	}

	private IActionResult AuthResult(Result<AuthTokens> result)
	{
		if (!result.IsSuccess)
			return MapError(result.Error!);

		AppendRefreshTokenCookie(result.Value!.RefreshToken);
		return Ok(new AuthResponse(result.Value.AccessToken, result.Value.User));
	}

	private void AppendRefreshTokenCookie(string refreshToken) =>
		Response.Cookies.Append(
			RefreshTokenCookieName,
			refreshToken,
			CreateRefreshTokenCookieOptions(
				DateTimeOffset.UtcNow.AddDays(jwtOptions.Value.RefreshTokenTtlDays)
			)
		);

	private void DeleteRefreshTokenCookie() =>
		Response.Cookies.Append(
			RefreshTokenCookieName,
			string.Empty,
			CreateRefreshTokenCookieOptions(DateTimeOffset.UnixEpoch, maxAge: TimeSpan.Zero)
		);

	private CookieOptions CreateRefreshTokenCookieOptions(
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
