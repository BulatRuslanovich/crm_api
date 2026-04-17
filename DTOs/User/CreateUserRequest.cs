namespace CrmWebApi.DTOs.User;

public record CreateUserRequest(
	string FirstName,
	string LastName,
	string Email,
	string Login,
	string Password,
	List<int> PolicyIds
);
