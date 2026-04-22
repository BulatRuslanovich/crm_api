using CrmWebApi.DTOs.User;

namespace CrmWebApi.DTOs.Auth;

public record AuthResponse(string AccessToken, UserResponse User);

public record AccessTokenResponse(string AccessToken);

public record AuthTokens(string AccessToken, string RefreshToken, UserResponse User);
