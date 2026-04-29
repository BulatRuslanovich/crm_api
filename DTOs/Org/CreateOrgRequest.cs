namespace CrmWebApi.DTOs.Org;

public record CreateOrgRequest(
	int OrgTypeId,
	string OrgName,
	string Inn,
	double? Latitude,
	double? Longitude,
	string Address
);
