namespace CrmWebApi.DTOs.Audit;

public record AuditLogQuery(
	string? EntityType,
	int? EntityId,
	string? Action,
	int? ChangedBy,
	DateTimeOffset? DateFrom,
	DateTimeOffset? DateTo
)
{
	public int Page { get; set; } = 1;
	public int PageSize { get; set; } = 50;
	public bool IncludeTotal { get; init; } = true;
}
