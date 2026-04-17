using CrmWebApi.Data;
using CrmWebApi.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CrmWebApi.Repositories.Impl;

public class PhysRepository(AppDbContext db) : IPhysRepository
{
	public IQueryable<Phys> QueryHard() =>
		db
			.Physes.Where(p => !p.IsDeleted)
			.Include(p => p.Spec)
			.Include(p => p.PhysOrgs)
				.ThenInclude(po => po.Org)
			.AsSplitQuery()
			.AsNoTracking();

	public IQueryable<Phys> QueryLite() => db.Physes.Where(p => !p.IsDeleted).AsQueryable();

	public async Task<Phys> AddAsync(Phys entity)
	{
		db.Physes.Add(entity);
		await db.SaveChangesAsync();
		return entity;
	}

	public async Task UpdateAsync(Phys entity)
	{
		db.Physes.Update(entity);
		await db.SaveChangesAsync();
	}

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

	public IQueryable<Spec> QuerySpecs() => db.Specs.Where(s => !s.IsDeleted).AsNoTracking();

	public async Task<Spec> AddSpecAsync(Spec entity)
	{
		db.Specs.Add(entity);
		await db.SaveChangesAsync();
		return entity;
	}

	public async Task<bool> SoftDeleteSpecAsync(int id)
	{
		var spec = await db.Specs.FirstOrDefaultAsync(s => s.SpecId == id);
		if (spec is null)
			return false;
		spec.IsDeleted = true;
		await db.SaveChangesAsync();
		return true;
	}
}
