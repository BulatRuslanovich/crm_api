using CrmWebApi.Common;
using CrmWebApi.Data;
using CrmWebApi.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CrmWebApi.Repositories.Impl;

public class ActivRepository(AppDbContext db) : IActivRepository
{
	public IQueryable<Activ> QueryForScope(Scope scope)
	{
		var baseQuery = db.Activs.Where(a => !a.IsDeleted).AsQueryable().AsSplitQuery().AsNoTracking();
		return scope.Visibility switch
		{
			Visibility.All => baseQuery,
			Visibility.Own => baseQuery.Where(a => a.UsrId == scope.CurrentUsrId),
			Visibility.Department => baseQuery.Where(a =>
				a.UsrId == scope.CurrentUsrId
				|| a.Usr.UsrDepartments.Any(ud =>
					ud.Department.UsrDepartments.Any(mine => mine.UsrId == scope.CurrentUsrId)
				)
			),
			_ => baseQuery.Where(_ => false),
		};
	}

	public async Task<Activ> AddWithDrugsAsync(Activ entity, IEnumerable<int> drugIds)
	{
		foreach (var drugId in drugIds)
			entity.ActivDrugs.Add(new ActivDrug { DrugId = drugId });
		db.Activs.Add(entity);
		await db.SaveChangesAsync();
		return entity;
	}

	public async Task UpdateAsync(Activ entity)
	{
		if (db.Entry(entity).State == EntityState.Detached)
			db.Activs.Update(entity);
		await db.SaveChangesAsync();
	}

	public async Task LinkDrugAsync(int activId, int drugId)
	{
		db.ActivDrugs.Add(new ActivDrug { ActivId = activId, DrugId = drugId });
		await db.SaveChangesAsync();
	}

	public async Task<bool> UnlinkDrugAsync(int activId, int drugId)
	{
		var deleted = await db
			.ActivDrugs.Where(ad => ad.ActivId == activId && ad.DrugId == drugId)
			.ExecuteDeleteAsync();
		return deleted > 0;
	}
}
