using CrmWebApi.DTOs.Org;

namespace CrmWebApi.DTOs.Phys;

public record PhysResponse(
	int PhysId,
	int SpecId,
	string SpecName,
	string FirstName,
	string LastName,
	string? MiddleName,
	string? Phone,
	string Email,
	List<OrgResponse> Orgs
);
