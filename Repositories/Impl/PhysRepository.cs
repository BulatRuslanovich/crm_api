using CrmWebApi.Data;
using CrmWebApi.Data.Entities;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Org;
using CrmWebApi.DTOs.Phys;
using CrmWebApi.DTOs.Spec;
using Microsoft.EntityFrameworkCore;

namespace CrmWebApi.Repositories.Impl;

public class PhysRepository(AppDbContext db) : IPhysRepository
{
	public async Task<PagedResponse<PhysResponse>> GetPagedAsync(
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
			.Select(p => ToResponse(p))
			.ToListAsync();

		return new PagedResponse<PhysResponse>(items, page, pageSize, total);
	}

	public Task<PhysResponse?> GetResponseByIdAsync(int id) =>
		QueryHard()
			.Where(p => p.PhysId == id)
			.Select(p => ToResponse(p))
			.FirstOrDefaultAsync();

	public Task<PhysAuditSnapshot?> GetAuditSnapshotAsync(int id) =>
		QueryLite()
			.Where(p => p.PhysId == id)
			.Select(p => new PhysAuditSnapshot(
				p.SpecId,
				p.PhysFirstname,
				p.PhysLastname,
				p.PhysMiddlename,
				p.PhysPhone,
				p.PhysEmail
			))
			.FirstOrDefaultAsync();

	public async Task<Phys> AddAsync(Phys entity)
	{
		db.Physes.Add(entity);
		await db.SaveChangesAsync();
		return entity;
	}

	public Task<int> UpdateAsync(int id, UpdatePhysRequest req) =>
		QueryLite()
			.Where(p => p.PhysId == id)
			.ExecuteUpdateAsync(s => s
				.SetProperty(p => p.SpecId, p => req.SpecId ?? p.SpecId)
				.SetProperty(p => p.PhysFirstname, p => req.FirstName ?? p.PhysFirstname)
				.SetProperty(p => p.PhysLastname, p => req.LastName ?? p.PhysLastname)
				.SetProperty(p => p.PhysMiddlename, p => req.MiddleName ?? p.PhysMiddlename)
				.SetProperty(p => p.PhysPhone, p => req.Phone ?? p.PhysPhone)
				.SetProperty(p => p.PhysEmail, p => req.Email ?? p.PhysEmail));

	public Task<int> SoftDeleteAsync(int id) =>
		QueryLite()
			.Where(p => p.PhysId == id)
			.ExecuteUpdateAsync(s => s.SetProperty(p => p.IsDeleted, true));

	public async Task LinkOrgAsync(int physId, int orgId)
	{
		db.PhysOrgs.Add(new PhysOrg { PhysId = physId, OrgId = orgId });
		await db.SaveChangesAsync();
	}

	public async Task<bool> UnlinkOrgAsync(int physId, int orgId)
	{
		var deleted = await db
			.PhysOrgs.Where(po => po.PhysId == physId && po.OrgId == orgId)
			.ExecuteDeleteAsync();
		return deleted > 0;
	}

	public async Task<IReadOnlyList<SpecResponse>> GetSpecsAsync(CancellationToken ct = default) =>
		await db.Specs
			.Where(s => !s.IsDeleted)
			.AsNoTracking()
			.Select(s => new SpecResponse(s.SpecId, s.SpecName))
			.ToListAsync(ct);

	public Task<SpecResponse?> GetSpecByIdAsync(int id) =>
		db.Specs
			.Where(s => !s.IsDeleted && s.SpecId == id)
			.AsNoTracking()
			.Select(s => new SpecResponse(s.SpecId, s.SpecName))
			.FirstOrDefaultAsync();

	public async Task<Spec> AddSpecAsync(Spec entity)
	{
		db.Specs.Add(entity);
		await db.SaveChangesAsync();
		return entity;
	}

	public async Task<bool> SoftDeleteSpecAsync(int id)
	{
		var affected = await db.Specs
			.Where(s => s.SpecId == id && !s.IsDeleted)
			.ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDeleted, true));
		return affected > 0;
	}

	private IQueryable<Phys> QueryHard() =>
		db
			.Physes.Where(p => !p.IsDeleted)
			.Include(p => p.Spec)
			.Include(p => p.PhysOrgs)
				.ThenInclude(po => po.Org)
			.AsSplitQuery()
			.AsNoTracking();

	private IQueryable<Phys> QueryLite() =>
		db.Physes.Where(p => !p.IsDeleted);

	private static IQueryable<Phys> ApplySearch(IQueryable<Phys> query, string? search)
	{
		if (string.IsNullOrEmpty(search))
			return query;

		var pattern = "%" + search + "%";
		const double threshold = 0.3;

		return query.Where(p =>
			EF.Functions.ILike(p.PhysFirstname, pattern)
			|| EF.Functions.ILike(p.PhysLastname, pattern)
			|| EF.Functions.ILike(p.PhysMiddlename ?? "", pattern)
			|| EF.Functions.TrigramsSimilarity(p.PhysFirstname, search) > threshold
			|| EF.Functions.TrigramsSimilarity(p.PhysLastname, search) > threshold
			|| EF.Functions.TrigramsSimilarity(p.PhysMiddlename ?? "", search) > threshold
		);
	}

	private static IQueryable<Phys> ApplySort(IQueryable<Phys> query, string? search)
	{
		if (string.IsNullOrEmpty(search))
			return query.OrderBy(p => p.PhysId);

		return query
			.OrderByDescending(p =>
				EF.Functions.TrigramsSimilarity(p.PhysLastname, search) >
				EF.Functions.TrigramsSimilarity(p.PhysFirstname, search)
					? EF.Functions.TrigramsSimilarity(p.PhysLastname, search)
					: EF.Functions.TrigramsSimilarity(p.PhysFirstname, search))
			.ThenBy(p => p.PhysId);
	}

	private static PhysResponse ToResponse(Phys p) =>
		new(
			p.PhysId,
			p.SpecId,
			p.Spec.SpecName,
			p.PhysFirstname,
			p.PhysLastname,
			p.PhysMiddlename,
			p.PhysPhone,
			p.PhysEmail,
			p.PhysOrgs.Select(po => new OrgResponse(
					po.OrgId,
					0,
					"-",
					po.Org.OrgName,
					"-",
					null,
					null,
					"-"
				))
				.ToList()
		);
}
