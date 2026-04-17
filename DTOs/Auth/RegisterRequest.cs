namespace CrmWebApi.DTOs.Auth;

public record RegisterRequest(
	string FirstName,
	string LastName,
	string Email,
	string Login,
	string Password
);
