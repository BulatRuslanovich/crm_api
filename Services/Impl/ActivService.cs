using System.Linq.Expressions;
using CrmWebApi.Common;
using CrmWebApi.Data.Entities;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Activ;
using CrmWebApi.DTOs.Drug;
using CrmWebApi.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CrmWebApi.Services.Impl;

public class ActivService(IActivRepository repo) : IActivService
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
			string pattern = "%" + searchValue + "%";
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

		var total = await query.CountAsync();
		var entity = await query
			.Skip((activQuery.Page - 1) * activQuery.PageSize)
			.Take(activQuery.PageSize)
			.Select(ToResponse)
			.ToListAsync();

		return new PagedResponse<ActivResponse>(
			entity,
			activQuery.Page,
			activQuery.PageSize,
			total
		);
	}

	public async Task<Result<ActivResponse>> GetByIdAsync(int id, Scope scope)
	{
		var activ = await repo.QueryForScope(scope)
			.Where(a => a.ActivId == id)
			.Select(ToResponse)
			.FirstOrDefaultAsync();

		if (activ is null)
			return Error.NotFound($"Активность {id} не найдена");

		return activ;
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
		};
		await repo.AddWithDrugsAsync(activ, req.DrugIds.Distinct());

		return await GetByIdAsync(activ.ActivId, Scope.ForAll(usrId));
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

		activ.StatusId = req.StatusId ?? activ.StatusId;
		activ.ActivStart = req.Start ?? activ.ActivStart;
		activ.ActivEnd = req.End ?? activ.ActivEnd;
		activ.ActivDescription = req.Description ?? activ.ActivDescription;

		await repo.UpdateAsync(activ);
		return await GetByIdAsync(id, Scope.ForAll(scope.CurrentUsrId));
	}

	public async Task<Result> DeleteAsync(int id, Scope scope)
	{
		var activ = await repo.QueryForScope(scope).FirstOrDefaultAsync(a => a.ActivId == id);
		if (activ is null)
			return Error.NotFound($"Активность {id} не найдена");

		activ.IsDeleted = true;
		await repo.UpdateAsync(activ);
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
