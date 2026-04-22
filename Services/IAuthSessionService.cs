using CrmWebApi.Common;
using CrmWebApi.Data.Entities;
using CrmWebApi.DTOs.Auth;

namespace CrmWebApi.Services;

public interface IAuthSessionService
{
	public Task<AuthTokens> IssueAsync(Usr user);
	public Task<Result<AuthTokens>> RefreshAsync(string refreshToken);
	public Task<Result> LogoutAsync(string refreshToken);
	public Task RevokeAllForUserAsync(int usrId);
}
