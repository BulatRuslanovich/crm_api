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
);
