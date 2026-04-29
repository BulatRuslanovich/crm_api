namespace CrmWebApi.DTOs.Audit;

public record AuditLogResponse(
	long AuditId,
	string EntityType,
	int EntityId,
	string Action,
	string? FieldName,
	string? OldValue,
	string? NewValue,
	int? ChangedBy,
	string? ChangedByLogin,
	DateTimeOffset ChangedAt
);
