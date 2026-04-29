using System.Globalization;
using System.Linq.Expressions;
using CrmWebApi.Common;
using CrmWebApi.Data;
using CrmWebApi.Data.Entities;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Activ;
using CrmWebApi.DTOs.Drug;
using CrmWebApi.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CrmWebApi.Services.Impl;

public class ActivService(IActivRepository repo, IAuditService audit, AppDbContext db) : IActivService
{
	private static readonly Expression<Func<Activ, ActivResponse>> ToResponse = a =>
		new ActivResponse(
			a.ActivId,
			a.UsrId,
			a.Usr.UsrLogin,
			a.OrgId,
			a.Org == null ? null : a.Org.OrgName,
			a.PhysId,
			a.Phys == null
				? null
				: (
					a.Phys.PhysLastname
					+ " "
					+ a.Phys.PhysFirstname
					+ (a.Phys.PhysMiddlename == null ? "" : " " + a.Phys.PhysMiddlename)
				),
			a.StatusId,
			a.Status.StatusName,
			a.ActivStart,
			a.ActivEnd,
			a.ActivDescription,
			a.ActivLatitude,
			a.ActivLongitude,
			a.ActivDrugs.Select(ad => new DrugResponse(
					ad.DrugId,
					ad.Drug.DrugName,
					ad.Drug.DrugBrand,
					ad.Drug.DrugForm
				))
				.ToList()
		);

	public async Task<Result<PagedResponse<ActivResponse>>> GetAllAsync(
		ActivQuery activQuery,
		Scope scope
	)
	{
		var statusesList = activQuery.Statuses?.ToList();
		var searchValue = activQuery.Search;
		var query = repo.QueryForScope(scope);

		if (!string.IsNullOrEmpty(searchValue))
		{
			var pattern = "%" + searchValue + "%";
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

		query = activQuery.SortBy switch
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

		var total = activQuery.IncludeTotal ? await query.CountAsync() : 0;

		var page = await query
			.Skip((activQuery.Page - 1) * activQuery.PageSize)
			.Take(activQuery.PageSize)
			.Select(a => new
			{
				a.ActivId,
				a.UsrId,
				a.Usr.UsrLogin,
				a.OrgId,
				OrgName = a.Org == null ? null : a.Org.OrgName,
				a.PhysId,
				PhysName = a.Phys == null
					? null
					: a.Phys.PhysLastname + " " + a.Phys.PhysFirstname +
					  (a.Phys.PhysMiddlename == null ? "" : " " + a.Phys.PhysMiddlename),
				a.StatusId,
				a.Status.StatusName,
				a.ActivStart,
				a.ActivEnd,
				a.ActivDescription,
				a.ActivLatitude,
				a.ActivLongitude,
			})
			.ToListAsync();

		var ids = page.ConvertAll(a => a.ActivId);
		var drugs = await repo.QueryDrugsForActivs(ids)
			.Select(ad => new
			{
				ad.ActivId,
				Drug = new DrugResponse(ad.DrugId, ad.Drug.DrugName, ad.Drug.DrugBrand, ad.Drug.DrugForm),
			})
			.ToListAsync();
		var drugMap = drugs.ToLookup(x => x.ActivId, x => x.Drug);

		var items = page.Select(a => new ActivResponse(
			a.ActivId, a.UsrId, a.UsrLogin,
			a.OrgId, a.OrgName,
			a.PhysId, a.PhysName,
			a.StatusId, a.StatusName,
			a.ActivStart, a.ActivEnd, a.ActivDescription,
			a.ActivLatitude, a.ActivLongitude,
			[.. drugMap[a.ActivId]]
		)).ToList();

		return new PagedResponse<ActivResponse>(items, activQuery.Page, activQuery.PageSize, total);
	}

	public async Task<Result<ActivResponse>> GetByIdAsync(int id, Scope scope)
	{
		var entity = await repo.QueryForScope(scope).Where(a => a.ActivId == id)
			.Select(a => new
			{
				a.ActivId,
				a.UsrId,
				a.Usr.UsrLogin,
				a.OrgId,
				OrgName = a.Org == null ? null : a.Org.OrgName,
				a.PhysId,
				PhysName = a.Phys == null
					? null
					: a.Phys.PhysLastname + " " + a.Phys.PhysFirstname +
					  (a.Phys.PhysMiddlename == null ? "" : " " + a.Phys.PhysMiddlename),
				a.StatusId,
				a.Status.StatusName,
				a.ActivStart,
				a.ActivEnd,
				a.ActivDescription,
				a.ActivLatitude,
				a.ActivLongitude,
			})
			.FirstOrDefaultAsync();

		if (entity is null)
			return Error.NotFound($"Активность {id} не найдена");

		var drugs = await repo.QueryDrugsForActivs([id])
			.Select(ad => new
			{
				ad.ActivId,
				Drug = new DrugResponse(ad.DrugId, ad.Drug.DrugName, ad.Drug.DrugBrand, ad.Drug.DrugForm),
			})
			.ToListAsync();
		var drugMap = drugs.ToLookup(x => x.ActivId, x => x.Drug);

		return new ActivResponse(
			entity.ActivId, entity.UsrId, entity.UsrLogin,
			entity.OrgId, entity.OrgName,
			entity.PhysId, entity.PhysName,
			entity.StatusId, entity.StatusName,
			entity.ActivStart, entity.ActivEnd, entity.ActivDescription,
			entity.ActivLatitude, entity.ActivLongitude,
			[.. drugMap[entity.ActivId]]
		);
	}

	public async Task<Result<ActivResponse>> CreateAsync(int usrId, CreateActivRequest req)
	{
		var activ = new Activ
		{
			UsrId = usrId,
			OrgId = req.OrgId,
			PhysId = req.PhysId,
			StatusId = req.StatusId,
			ActivStart = req.Start,
			ActivEnd = req.End,
			ActivDescription = req.Description,
			ActivLatitude = req.Latitude,
			ActivLongitude = req.Longitude,
		};

		await using var tx = await db.Database.BeginTransactionAsync();
		var added = await repo.AddAsync(activ);
		await audit.LogCreateAsync(AuditEntityType.Activ, added.ActivId);
		await tx.CommitAsync();

		return await GetByIdAsync(added.ActivId, Scope.ForAll(usrId));
	}

	public async Task<Result<ActivResponse>> UpdateAsync(
		int id,
		UpdateActivRequest req,
		Scope scope
	)
	{
		var activ = await repo.QueryForScope(scope).FirstOrDefaultAsync(a => a.ActivId == id);
		if (activ is null)
			return Error.NotFound($"Активность {id} не найдена");

		var diffs = new List<AuditDiff>();
		if (req.StatusId is { } st && st != activ.StatusId)
			diffs.Add(new("status_id", activ.StatusId.ToString(), st.ToString()));
		if (req.Start is { } start && start != activ.ActivStart)
			diffs.Add(new("activ_start", activ.ActivStart?.ToString("O"), start.ToString("O")));
		if (req.End is { } end && end != activ.ActivEnd)
			diffs.Add(new("activ_end", activ.ActivEnd?.ToString("O"), end.ToString("O")));
		if (req.Description is { } desc && desc != activ.ActivDescription)
			diffs.Add(new("activ_description", activ.ActivDescription, desc));
		if (req.Latitude is { } lat && lat != activ.ActivLatitude)
			diffs.Add(new("activ_latitude", activ.ActivLatitude?.ToString(CultureInfo.InvariantCulture), lat.ToString(CultureInfo.InvariantCulture)));
		if (req.Longitude is { } lon && lon != activ.ActivLongitude)
			diffs.Add(new("activ_longitude", activ.ActivLongitude?.ToString(CultureInfo.InvariantCulture), lon.ToString(CultureInfo.InvariantCulture)));

		if (diffs.Count == 0)
			return await GetByIdAsync(id, Scope.ForAll(scope.CurrentUsrId));

		activ.StatusId = req.StatusId ?? activ.StatusId;
		activ.ActivStart = req.Start ?? activ.ActivStart;
		activ.ActivEnd = req.End ?? activ.ActivEnd;
		activ.ActivDescription = req.Description ?? activ.ActivDescription;
		activ.ActivLatitude = req.Latitude ?? activ.ActivLatitude;
		activ.ActivLongitude = req.Longitude ?? activ.ActivLongitude;

		await using var tx = await db.Database.BeginTransactionAsync();
		await repo.UpdateAsync(activ);
		await audit.LogUpdateAsync(AuditEntityType.Activ, id, diffs);
		await tx.CommitAsync();
		return await GetByIdAsync(id, Scope.ForAll(scope.CurrentUsrId));
	}

	public async Task<Result> DeleteAsync(int id, Scope scope)
	{
		await using var tx = await db.Database.BeginTransactionAsync();
		var affected = await repo.QueryForScope(scope)
			.Where(a => a.ActivId == id)
			 .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDeleted, true));

		if (affected == 0)
			return Error.NotFound($"Активность {id} не найдена");

		await audit.LogDeleteAsync(AuditEntityType.Activ, id);
		await tx.CommitAsync();
		return Result.Success();
	}

	public async Task<Result> LinkDrugAsync(int activId, int drugId, Scope scope)
	{
		var exists = await repo.QueryForScope(scope).AnyAsync(a => a.ActivId == activId);
		if (!exists)
			return Error.NotFound($"Активность {activId} не найдена");

		await repo.LinkDrugAsync(activId, drugId);
		return Result.Success();
	}

	public async Task<Result> UnlinkDrugAsync(int activId, int drugId, Scope scope)
	{
		var exists = await repo.QueryForScope(scope).AnyAsync(a => a.ActivId == activId);
		if (!exists)
			return Error.NotFound($"Активность {activId} не найдена");

		var found = await repo.UnlinkDrugAsync(activId, drugId);
		if (!found)
			return Error.NotFound("Связь не найдена");
		return Result.Success();
	}
}
