using CrmWebApi.DTOs.Drug;

namespace CrmWebApi.DTOs.Activ;

public record ActivResponse(
	int ActivId,
	int UsrId,
	string UsrLogin,
	int? OrgId,
	string? OrgName,
	int? PhysId,
	string? PhysName,
	int StatusId,
	string StatusName,
	DateTimeOffset? Start,
	DateTimeOffset? End,
	string Description,
	double? Latitude,
	double? Longitude,
	List<DrugResponse> Drugs
);
