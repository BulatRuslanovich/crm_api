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
)
{
	public static PhysResponse From(Data.Entities.Phys phys) =>
		new(
			phys.PhysId,
			phys.SpecId,
			phys.Spec.SpecName,
			phys.PhysFirstname,
			phys.PhysLastname,
			phys.PhysMiddlename,
			phys.PhysPhone,
			phys.PhysEmail,
			[
				.. phys.PhysOrgs.Select(p => new OrgResponse(
					p.OrgId,
					0,
					"-",
					p.Org.OrgName,
					"-",
					0,
					0,
					"-"
				)),
			]
		);
}
