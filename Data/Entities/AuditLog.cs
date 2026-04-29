using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrmWebApi.Data.Entities;

[Table("audit_log")]
public class AuditLog
{
	[Key]
	[Column("audit_id")]
	public long AuditId { get; init; }

	[MaxLength(50)]
	[Column("entity_type")]
	public string EntityType { get; init; } = null!;

	[Column("entity_id")]
	public int EntityId { get; init; }

	[MaxLength(20)]
	[Column("action")]
	public string Action { get; init; } = null!;

	[MaxLength(100)]
	[Column("field_name")]
	public string? FieldName { get; init; }

	[Column("old_value")]
	public string? OldValue { get; init; }

	[Column("new_value")]
	public string? NewValue { get; init; }

	[Column("changed_by")]
	public int? ChangedBy { get; init; }

	[Column("changed_at")]
	public DateTimeOffset ChangedAt { get; init; }
}
