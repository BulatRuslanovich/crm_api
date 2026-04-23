namespace CrmWebApi.DTOs.Activ;

public record CreateActivRequest(
	int? OrgId,
	int? PhysId,
	int StatusId,
	DateTimeOffset Start,
	DateTimeOffset? End,
	string Description
);
