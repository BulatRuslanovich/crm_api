namespace CrmWebApi.DTOs.Org;

public record UpdateOrgRequest(
	int? OrgTypeId,
	string? OrgName,
	string? Inn,
	double? Latitude,
	double? Longitude,
	string? Address
);
