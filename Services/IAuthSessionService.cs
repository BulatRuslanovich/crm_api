using CrmWebApi.Common;
using CrmWebApi.DTOs.Auth;

namespace CrmWebApi.Services;

public interface IAuthSessionService
{
	public Task<Result<AuthTokens>> IssueAsync(int usrId);
	public Task<Result<AuthTokens>> RefreshAsync(string refreshToken);
	public Task<Result> LogoutAsync(string refreshToken);
	public Task RevokeAllForUserAsync(int usrId);
}
