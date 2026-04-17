namespace CrmWebApi.DTOs.Auth;

public record ConfirmEmailRequest(string Email, string Code);
