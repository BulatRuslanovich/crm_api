namespace CrmWebApi.DTOs.Activ;

public record UpdateActivRequest(
	int? StatusId,
	DateTimeOffset? Start,
	DateTimeOffset? End,
	string? Description,
	double? Latitude,
	double? Longitude
);
