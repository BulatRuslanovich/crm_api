using CrmWebApi.Data;
using CrmWebApi.Data.Entities;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Org;
using CrmWebApi.DTOs.OrgType;
using Microsoft.EntityFrameworkCore;

namespace CrmWebApi.Repositories.Impl;

public class OrgRepository(AppDbContext db) : IOrgRepository
{
	public async Task<PagedResponse<OrgResponse>> GetPagedAsync(
		int page,
		int pageSize,
		string? search,
		bool includeTotal
	)
	{
		var query = QueryHard();
		query = ApplySearch(query, search);

		var total = includeTotal ? await query.CountAsync() : 0;
		var ordered = ApplySort(query, search);

		var items = await ordered
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.Select(o => ToResponse(o))
			.ToListAsync();
		return new PagedResponse<OrgResponse>(items, page, pageSize, total);
	}

	public Task<OrgResponse?> GetResponseByIdAsync(int id) =>
		QueryHard()
			.Where(o => o.OrgId == id)
			.Select(o => ToResponse(o))
			.FirstOrDefaultAsync();

	public Task<OrgAuditSnapshot?> GetAuditSnapshotAsync(int id) =>
		QueryLite()
			.Where(o => o.OrgId == id)
			.Select(o => new OrgAuditSnapshot(
				o.OrgTypeId,
				o.OrgName,
				o.OrgInn,
				o.OrgLatitude,
				o.OrgLongitude,
				o.OrgAddress
			))
			.FirstOrDefaultAsync();

	public async Task<Organization> AddAsync(Organization entity)
	{
		db.Orgs.Add(entity);
		await db.SaveChangesAsync();
		return entity;
	}

	public Task<int> UpdateAsync(int id, UpdateOrgRequest req) =>
		QueryLite()
			.Where(o => o.OrgId == id)
			.ExecuteUpdateAsync(s => s
				.SetProperty(o => o.OrgTypeId, o => req.OrgTypeId ?? o.OrgTypeId)
				.SetProperty(o => o.OrgName, o => req.OrgName ?? o.OrgName)
				.SetProperty(o => o.OrgInn, o => req.Inn ?? o.OrgInn)
				.SetProperty(o => o.OrgLatitude, o => req.Latitude ?? o.OrgLatitude)
				.SetProperty(o => o.OrgLongitude, o => req.Longitude ?? o.OrgLongitude)
				.SetProperty(o => o.OrgAddress, o => req.Address ?? o.OrgAddress));

	public Task<int> SoftDeleteAsync(int id) =>
		QueryLite()
			.Where(o => o.OrgId == id)
			.ExecuteUpdateAsync(s => s.SetProperty(o => o.IsDeleted, true));

	public async Task<IReadOnlyList<OrgTypeResponse>> GetOrgTypesAsync(CancellationToken ct = default) =>
		await db.OrgTypes
			.AsNoTracking()
			.Select(ot => new OrgTypeResponse(ot.OrgTypeId, ot.OrgTypeName))
			.ToListAsync(ct);

	private IQueryable<Organization> QueryHard() =>
		db.Orgs.Where(o => !o.IsDeleted).Include(o => o.OrgType).AsNoTracking();

	private IQueryable<Organization> QueryLite() =>
		db.Orgs.Where(o => !o.IsDeleted);

	private static IQueryable<Organization> ApplySearch(IQueryable<Organization> query, string? search)
	{
		if (string.IsNullOrEmpty(search))
			return query;

		var pattern = "%" + search + "%";
		const double threshold = 0.3;

		return query.Where(o =>
			EF.Functions.ILike(o.OrgName, pattern)
			|| EF.Functions.ILike(o.OrgAddress, pattern)
			|| EF.Functions.TrigramsSimilarity(o.OrgName, search) > threshold
			|| EF.Functions.TrigramsSimilarity(o.OrgAddress, search) > threshold
		);
	}

	private static IQueryable<Organization> ApplySort(IQueryable<Organization> query, string? search)
	{
		if (string.IsNullOrEmpty(search))
			return query.OrderBy(o => o.OrgId);

		return query
			.OrderByDescending(o =>
				EF.Functions.TrigramsSimilarity(o.OrgName, search) >
				EF.Functions.TrigramsSimilarity(o.OrgAddress, search)
					? EF.Functions.TrigramsSimilarity(o.OrgName, search)
					: EF.Functions.TrigramsSimilarity(o.OrgAddress, search))
			.ThenBy(o => o.OrgId);
	}

	private static OrgResponse ToResponse(Organization o) =>
		new(
			o.OrgId,
			o.OrgTypeId,
			o.OrgType.OrgTypeName,
			o.OrgName,
			o.OrgInn,
			o.OrgLatitude,
			o.OrgLongitude,
			o.OrgAddress
		);
}
