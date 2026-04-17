using System.Security.Claims;
using CrmWebApi.DTOs.Auth;
using CrmWebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CrmWebApi.Controllers;

[Route("api/auth")]
[Tags("Auth")]
[EnableRateLimiting("auth")]
public class AuthController(IAuthService service) : ApiController
{
	[HttpPost("register")]
	[AllowAnonymous]
	[EndpointSummary("Register a new user")]
	[EndpointDescription(
		"Creates a user account and sends a 6-digit OTP code to the provided email. The account must be confirmed before login."
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
		"Verifies the 6-digit code sent during registration. Returns JWT tokens on success."
	)]
	[ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
	[ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
	public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest req) =>
		FromResult(await service.ConfirmEmailAsync(req));

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
		"Authenticates with login/password and returns JWT access + refresh tokens."
	)]
	[ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
	[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
	public async Task<IActionResult> Login([FromBody] LoginRequest req) =>
		FromResult(await service.LoginAsync(req));

	[HttpPost("refresh")]
	[AllowAnonymous]
	[EndpointSummary("Refresh tokens")]
	[EndpointDescription(
		"Exchanges a valid refresh token for a new access + refresh token pair. The old refresh token is consumed."
	)]
	[ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
	[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
	public async Task<IActionResult> Refresh([FromBody] string refreshToken) =>
		FromResult(await service.RefreshAsync(refreshToken));

	[HttpPost("logout")]
	[Authorize]
	[EndpointSummary("Logout")]
	[EndpointDescription("Revokes the provided refresh token. Requires authentication.")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	public async Task<IActionResult> Logout([FromBody] string refreshToken)
	{
		var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
		return FromResult(await service.LogoutAsync(refreshToken, currentUserId));
	}
}
