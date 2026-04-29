using CrmWebApi.Common;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Audit;

namespace CrmWebApi.Services;

public interface IAuditService
{
	public Task LogCreateAsync(string entityType, int entityId);
	public Task LogUpdateAsync(string entityType, int entityId, IReadOnlyList<AuditDiff> diffs);
	public Task LogDeleteAsync(string entityType, int entityId);

	public Task<Result<PagedResponse<AuditLogResponse>>> GetAllAsync(AuditLogQuery query);
}

public readonly record struct AuditDiff(string FieldName, string? OldValue, string? NewValue);

public static class AuditEntityType
{
	public const string Activ = "activ";
	public const string Org = "org";
	public const string Phys = "phys";
}

public static class AuditAction
{
	public const string Insert = "INSERT";
	public const string Update = "UPDATE";
	public const string Delete = "DELETE";
}
