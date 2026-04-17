using CrmWebApi.Common;
using CrmWebApi.DTOs.Auth;

namespace CrmWebApi.Services;

public interface IAuthService
{
	public Task<Result<PendingConfirmationResponse>> RegisterAsync(RegisterRequest req);
	public Task<Result<AuthResponse>> ConfirmEmailAsync(ConfirmEmailRequest req);
	public Task<Result> ResendConfirmationAsync(string email);
	public Task<Result<AuthResponse>> LoginAsync(LoginRequest req);
	public Task<Result<AuthResponse>> RefreshAsync(string refreshToken);
	public Task<Result> LogoutAsync(string refreshToken, int currentUserId);
	public Task<Result> ForgotPasswordAsync(string email);
	public Task<Result> ResetPasswordAsync(ResetPasswordRequest req);
}
