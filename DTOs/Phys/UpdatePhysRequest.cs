namespace CrmWebApi.DTOs.Phys;

public record UpdatePhysRequest(
	int? SpecId,
	string? FirstName,
	string? LastName,
	string? MiddleName,
	string? Phone,
	string? Email
);
