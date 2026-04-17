namespace CrmWebApi.DTOs.Phys;

public record CreatePhysRequest(
	int SpecId,
	string FirstName,
	string LastName,
	string MiddleName,
	string Phone,
	string Email
);
