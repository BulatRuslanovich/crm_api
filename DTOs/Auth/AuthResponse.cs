using CrmWebApi.DTOs.User;

namespace CrmWebApi.DTOs.Auth;

public record AuthResponse(string AccessToken, string RefreshToken, UserResponse User);
