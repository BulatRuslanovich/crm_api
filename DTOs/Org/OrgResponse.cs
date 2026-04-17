using CrmWebApi.Data.Entities;

namespace CrmWebApi.DTOs.Org;

public record OrgResponse(
	int OrgId,
	int OrgTypeId,
	string OrgTypeName,
	string OrgName,
	string Inn,
	double Latitude,
	double Longitude,
	string Address
)
{
	public static OrgResponse From(Organization o) =>
		new(
			o.OrgId,
			o.OrgTypeId,
			o.OrgType.OrgTypeName,
			o.OrgName,
			o.OrgInn,
			o.OrgLatitude,
			o.OrgLongitude,
			o.OrgAddress
		);
}
