using CrmWebApi.Data;
using CrmWebApi.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CrmWebApi.Repositories.Impl;

public class OrgRepository(AppDbContext db) : IOrgRepository
{
	public IQueryable<Organization> QueryHard() => db.Orgs.Where(o => !o.IsDeleted).Include(o => o.OrgType).AsNoTracking();

	public IQueryable<Organization> QueryLite() => db.Orgs.Where(o => !o.IsDeleted).AsQueryable();

	public IQueryable<OrgType> QueryOrgTypes() => db.OrgTypes.AsNoTracking();

	public async Task<Organization> AddAsync(Organization entity)
	{
		db.Orgs.Add(entity);
		await db.SaveChangesAsync();
		return entity;
	}

	public async Task UpdateAsync(Organization entity)
	{
		if (db.Entry(entity).State == EntityState.Detached)
			db.Orgs.Update(entity);
		await db.SaveChangesAsync();
	}
}
