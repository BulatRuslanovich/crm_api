using CrmWebApi.Common;
using CrmWebApi.Data;
using CrmWebApi.Data.Entities;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Audit;
using Microsoft.EntityFrameworkCore;

namespace CrmWebApi.Services.Impl;

public sealed class AuditService(AppDbContext db, ICurrentUserService currentUser) : IAuditService
{
	public Task LogCreateAsync(string entityType, int entityId) =>
		WriteSingleAsync(entityType, entityId, AuditAction.Insert, fieldName: null, oldValue: null, newValue: null);

	public async Task LogUpdateAsync(string entityType, int entityId, IReadOnlyList<AuditDiff> diffs)
	{
		if (diffs.Count == 0)
			return;

		var changedBy = currentUser.UsrId;
		var now = DateTimeOffset.UtcNow;
		foreach (var diff in diffs)
		{
			db.AuditLogs.Add(new AuditLog
			{
				EntityType = entityType,
				EntityId = entityId,
				Action = AuditAction.Update,
				FieldName = diff.FieldName,
				OldValue = diff.OldValue,
				NewValue = diff.NewValue,
				ChangedBy = changedBy,
				ChangedAt = now,
			});
		}
		await db.SaveChangesAsync();
	}

	public Task LogDeleteAsync(string entityType, int entityId) =>
		WriteSingleAsync(entityType, entityId, AuditAction.Delete, fieldName: null, oldValue: null, newValue: null);

	public async Task<Result<PagedResponse<AuditLogResponse>>> GetAllAsync(AuditLogQuery query)
	{
		var q = db.AuditLogs.AsNoTracking().AsQueryable();

		if (!string.IsNullOrWhiteSpace(query.EntityType))
			q = q.Where(a => a.EntityType == query.EntityType);
		if (query.EntityId is { } eid)
			q = q.Where(a => a.EntityId == eid);
		if (!string.IsNullOrWhiteSpace(query.Action))
			q = q.Where(a => a.Action == query.Action);
		if (query.ChangedBy is { } cb)
			q = q.Where(a => a.ChangedBy == cb);
		if (query.DateFrom is { } from)
			q = q.Where(a => a.ChangedAt >= from);
		if (query.DateTo is { } to)
			q = q.Where(a => a.ChangedAt <= to);

		var total = query.IncludeTotal ? await q.CountAsync() : 0;

		var items = await q
			.OrderByDescending(a => a.ChangedAt)
			.ThenByDescending(a => a.AuditId)
			.Skip((query.Page - 1) * query.PageSize)
			.Take(query.PageSize)
			.Select(a => new AuditLogResponse(
				a.AuditId,
				a.EntityType,
				a.EntityId,
				a.Action,
				a.FieldName,
				a.OldValue,
				a.NewValue,
				a.ChangedBy,
				a.ChangedBy == null
					? null
					: db.Usrs.Where(u => u.UsrId == a.ChangedBy).Select(u => u.UsrLogin).FirstOrDefault(),
				a.ChangedAt
			))
			.ToListAsync();

		return new PagedResponse<AuditLogResponse>(items, query.Page, query.PageSize, total);
	}

	private async Task WriteSingleAsync(
		string entityType,
		int entityId,
		string action,
		string? fieldName,
		string? oldValue,
		string? newValue
	)
	{
		db.AuditLogs.Add(new AuditLog
		{
			EntityType = entityType,
			EntityId = entityId,
			Action = action,
			FieldName = fieldName,
			OldValue = oldValue,
			NewValue = newValue,
			ChangedBy = currentUser.UsrId,
			ChangedAt = DateTimeOffset.UtcNow,
		});
		await db.SaveChangesAsync();
	}
}
