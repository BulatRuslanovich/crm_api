using System.Globalization;
using CrmWebApi.Common;
using CrmWebApi.Data;
using CrmWebApi.Data.Entities;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Org;
using CrmWebApi.DTOs.OrgType;
using CrmWebApi.Repositories;
using Microsoft.Extensions.Caching.Hybrid;

namespace CrmWebApi.Services.Impl;

public class OrgService(IOrgRepository repo, HybridCache cache, IAuditService audit, AppDbContext db)
	: IOrgService
{
	private static readonly string[] TypeTags = ["org-types"];
	private static readonly HybridCacheEntryOptions RefOptions = new()
	{
		Expiration = TimeSpan.FromMinutes(10),
	};

	public async Task<Result<PagedResponse<OrgResponse>>> GetAllAsync(
		int page,
		int pageSize,
		string? search = null,
		bool includeTotal = true
	) => await repo.GetPagedAsync(page, pageSize, search, includeTotal);

	public async Task<Result<OrgResponse>> GetByIdAsync(int id)
	{
		var org = await repo.GetResponseByIdAsync(id);
		return org is null ? Error.NotFound($"Организация {id} не найдена") : org;
	}

	public async Task<Result<OrgResponse>> CreateAsync(CreateOrgRequest req)
	{
		var org = new Organization
		{
			OrgTypeId = req.OrgTypeId,
			OrgName = req.OrgName,
			OrgInn = req.Inn,
			OrgLatitude = req.Latitude,
			OrgLongitude = req.Longitude,
			OrgAddress = req.Address,
		};

		await using var tx = await db.Database.BeginTransactionAsync();
		await repo.AddAsync(org);
		await audit.LogCreateAsync(AuditEntityType.Org, org.OrgId);
		await tx.CommitAsync();

		return await GetByIdAsync(org.OrgId);
	}

	public async Task<Result<OrgResponse>> UpdateAsync(int id, UpdateOrgRequest req)
	{
		var old = await repo.GetAuditSnapshotAsync(id);
		if (old is null)
			return Error.NotFound($"Организация {id} не найдена");

		var diffs = new List<AuditDiff>();
		if (req.OrgTypeId is { } t && t != old.OrgTypeId)
			diffs.Add(new("org_type_id", old.OrgTypeId.ToString(), t.ToString()));
		if (req.OrgName is { } n && n != old.OrgName)
			diffs.Add(new("org_name", old.OrgName, n));
		if (req.Inn is { } inn && inn != old.OrgInn)
			diffs.Add(new("org_inn", old.OrgInn, inn));
		if (req.Latitude is { } lat && lat != old.OrgLatitude)
			diffs.Add(new("org_latitude", old.OrgLatitude?.ToString(CultureInfo.InvariantCulture), lat.ToString(CultureInfo.InvariantCulture)));
		if (req.Longitude is { } lon && lon != old.OrgLongitude)
			diffs.Add(new("org_longitude", old.OrgLongitude?.ToString(CultureInfo.InvariantCulture), lon.ToString(CultureInfo.InvariantCulture)));
		if (req.Address is { } addr && addr != old.OrgAddress)
			diffs.Add(new("org_address", old.OrgAddress, addr));

		if (diffs.Count > 0)
		{
			await using var tx = await db.Database.BeginTransactionAsync();
			await repo.UpdateAsync(id, req);
			await audit.LogUpdateAsync(AuditEntityType.Org, id, diffs);
			await tx.CommitAsync();
		}

		return await GetByIdAsync(id);
	}

	public async Task<Result> DeleteAsync(int id)
	{
		await using var tx = await db.Database.BeginTransactionAsync();
		var affected = await repo.SoftDeleteAsync(id);
		if (affected == 0)
			return Error.NotFound($"Организация {id} не найдена");

		await audit.LogDeleteAsync(AuditEntityType.Org, id);
		await tx.CommitAsync();
		return Result.Success();
	}

	public async Task<Result<IEnumerable<OrgTypeResponse>>> GetAllTypesAsync() =>
		Result<IEnumerable<OrgTypeResponse>>.Success(
			await cache.GetOrCreateAsync(
				"org-types",
				async ct => (IEnumerable<OrgTypeResponse>)await repo.GetOrgTypesAsync(ct),
				RefOptions,
				TypeTags
			)
		);
}
