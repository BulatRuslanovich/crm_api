using CrmWebApi.Common;
using CrmWebApi.Data;
using CrmWebApi.Data.Entities;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Activ;
using CrmWebApi.DTOs.Drug;
using Microsoft.EntityFrameworkCore;

namespace CrmWebApi.Repositories.Impl;

public class ActivRepository(AppDbContext db) : IActivRepository
{
	public async Task<PagedResponse<ActivResponse>> GetPagedForScopeAsync(
		ActivQuery activQuery,
		Scope scope
	)
	{
		var statusesList = activQuery.Statuses?.ToList();
		var query = ApplyFilters(QueryForScope(scope), activQuery, statusesList);
		query = ApplySort(query, activQuery);

		var total = activQuery.IncludeTotal ? await query.CountAsync() : 0;

		var page = await query
			.Skip((activQuery.Page - 1) * activQuery.PageSize)
			.Take(activQuery.PageSize)
			.Select(a => new ActivProjection(
				a.ActivId,
				a.UsrId,
				a.Usr.UsrLogin,
				a.OrgId,
				a.Org == null ? null : a.Org.OrgName,
				a.PhysId,
				a.Phys == null
					? null
					: a.Phys.PhysLastname + " " + a.Phys.PhysFirstname +
					  (a.Phys.PhysMiddlename == null ? "" : " " + a.Phys.PhysMiddlename),
				a.StatusId,
				a.Status.StatusName,
				a.ActivStart,
				a.ActivEnd,
				a.ActivDescription,
				a.ActivLatitude,
				a.ActivLongitude
			))
			.ToListAsync();

		var items = await HydrateDrugsAsync(page);
		return new PagedResponse<ActivResponse>(items, activQuery.Page, activQuery.PageSize, total);
	}

	public async Task<ActivResponse?> GetResponseByIdForScopeAsync(int id, Scope scope)
	{
		var row = await QueryForScope(scope)
			.Where(a => a.ActivId == id)
			.Select(a => new ActivProjection(
				a.ActivId,
				a.UsrId,
				a.Usr.UsrLogin,
				a.OrgId,
				a.Org == null ? null : a.Org.OrgName,
				a.PhysId,
				a.Phys == null
					? null
					: a.Phys.PhysLastname + " " + a.Phys.PhysFirstname +
					  (a.Phys.PhysMiddlename == null ? "" : " " + a.Phys.PhysMiddlename),
				a.StatusId,
				a.Status.StatusName,
				a.ActivStart,
				a.ActivEnd,
				a.ActivDescription,
				a.ActivLatitude,
				a.ActivLongitude
			))
			.FirstOrDefaultAsync();

		if (row is null)
			return null;

		return (await HydrateDrugsAsync([row])).Single();
	}

	public Task<Activ?> GetForUpdateAsync(int id, Scope scope) =>
		QueryForScope(scope, asNoTracking: false).FirstOrDefaultAsync(a => a.ActivId == id);

	public Task<bool> ExistsInScopeAsync(int id, Scope scope) =>
		QueryForScope(scope).AnyAsync(a => a.ActivId == id);

	public async Task<Activ> AddAsync(Activ entity)
	{
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

	public Task<int> SoftDeleteAsync(int id, Scope scope) =>
		QueryForScope(scope, asNoTracking: false)
			.Where(a => a.ActivId == id)
			.ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDeleted, true));

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

	private IQueryable<Activ> QueryForScope(Scope scope, bool asNoTracking = true)
	{
		var baseQuery = db.Activs.Where(a => !a.IsDeleted).AsQueryable().AsSplitQuery();
		if (asNoTracking)
			baseQuery = baseQuery.AsNoTracking();

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

	private static IQueryable<Activ> ApplyFilters(
		IQueryable<Activ> query,
		ActivQuery activQuery,
		IReadOnlyCollection<int>? statusesList
	)
	{
		if (!string.IsNullOrEmpty(activQuery.Search))
		{
			var pattern = "%" + activQuery.Search + "%";
			query = query.Where(a =>
				EF.Functions.ILike(a.ActivDescription, pattern)
				|| (a.Org != null && EF.Functions.ILike(a.Org.OrgName, pattern))
				|| (
					a.Phys != null
					&& (
						EF.Functions.ILike(a.Phys.PhysLastname, pattern)
						|| EF.Functions.ILike(a.Phys.PhysFirstname, pattern)
						|| EF.Functions.ILike(a.Phys.PhysMiddlename ?? "", pattern)
					)
				)
				|| a.ActivDrugs.Any(ad => EF.Functions.ILike(ad.Drug.DrugName, pattern))
			);
		}

		if (activQuery.UsrId is not null)
			query = query.Where(a => a.UsrId == activQuery.UsrId);

		if (activQuery.DateFrom is not null)
			query = query.Where(a => a.ActivStart >= activQuery.DateFrom);

		if (activQuery.DateTo is not null)
			query = query.Where(a => a.ActivStart <= activQuery.DateTo);

		if (statusesList is { Count: > 0 })
			query = query.Where(a => statusesList.Contains(a.StatusId));

		return query;
	}

	private static IQueryable<Activ> ApplySort(IQueryable<Activ> query, ActivQuery activQuery) =>
		activQuery.SortBy switch
		{
			ActivSortBy.Start => activQuery.SortDesc
				? query.OrderByDescending(a => a.ActivStart)
				: query.OrderBy(a => a.ActivStart),
			ActivSortBy.End => activQuery.SortDesc
				? query.OrderByDescending(a => a.ActivEnd)
				: query.OrderBy(a => a.ActivEnd),
			ActivSortBy.Status => activQuery.SortDesc
				? query.OrderByDescending(a => a.Status.StatusName)
				: query.OrderBy(a => a.Status.StatusName),
			_ => query.OrderBy(a => a.ActivId),
		};

	private async Task<List<ActivResponse>> HydrateDrugsAsync(IReadOnlyList<ActivProjection> rows)
	{
		var ids = rows.Select(a => a.ActivId).ToArray();
		var drugs = await db.ActivDrugs
			.Where(ad => ids.Contains(ad.ActivId))
			.AsNoTracking()
			.Select(ad => new
			{
				ad.ActivId,
				Drug = new DrugResponse(ad.DrugId, ad.Drug.DrugName, ad.Drug.DrugBrand, ad.Drug.DrugForm),
			})
			.ToListAsync();
		var drugMap = drugs.ToLookup(x => x.ActivId, x => x.Drug);

		return rows.Select(a => new ActivResponse(
			a.ActivId, a.UsrId, a.UsrLogin,
			a.OrgId, a.OrgName,
			a.PhysId, a.PhysName,
			a.StatusId, a.StatusName,
			a.ActivStart, a.ActivEnd, a.ActivDescription,
			a.ActivLatitude, a.ActivLongitude,
			[.. drugMap[a.ActivId]]
		)).ToList();
	}

	private sealed record ActivProjection(
		int ActivId,
		int UsrId,
		string UsrLogin,
		int? OrgId,
		string? OrgName,
		int? PhysId,
		string? PhysName,
		int StatusId,
		string StatusName,
		DateTimeOffset? ActivStart,
		DateTimeOffset? ActivEnd,
		string ActivDescription,
		double? ActivLatitude,
		double? ActivLongitude
	);
}
