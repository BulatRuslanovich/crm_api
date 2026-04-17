namespace CrmWebApi.DTOs.User;

public record ChangePasswordRequest(string OldPassword, string NewPassword);
