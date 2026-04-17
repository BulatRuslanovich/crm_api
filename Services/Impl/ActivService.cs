using CrmWebApi.Common;
using CrmWebApi.Data.Entities;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Activ;
using CrmWebApi.DTOs.Drug;
using CrmWebApi.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace CrmWebApi.Services.Impl;

public class ActivService(IActivRepository repo, HybridCache cache, ILogger<ActivService> logger)
	: IActivService
{
	private static readonly string[] Tags = ["activs"];

	public async Task<Result<PagedResponse<ActivResponse>>> GetAllAsync(
		ActivQuery activQuery,
		ActivScope scope
	)
	{
		var statusesList = activQuery.Statuses?.ToList();
		var searchValue = activQuery.Search;

		return await cache.GetOrCreateAsync(
			$"activs:{scope.Visibility}:{scope.CurrentUsrId}:{activQuery.Page}:{activQuery.PageSize}:{searchValue}:{activQuery.SortBy}:{activQuery.SortDesc}:{string.Join(",", statusesList ?? [])}:{activQuery.DateFrom:O}:{activQuery.DateTo:O}",
			async ct =>
			{
				var query = repo.QueryForScope(scope);

				if (!string.IsNullOrEmpty(searchValue))
				{
					string pattern = "%" + searchValue + "%";
					query = query.Where(a =>
						EF.Functions.ILike(a.ActivDescription ?? "", pattern)
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

				var total = await query.CountAsync(ct);
				var entity = await query
					.Skip((activQuery.Page - 1) * activQuery.PageSize)
					.Take(activQuery.PageSize)
					.Select(a => new ActivResponse(
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
						a.ActivDrugs.Select(a => new DrugResponse(
								a.DrugId,
								a.Drug.DrugName,
								a.Drug.DrugBrand,
								a.Drug.DrugForm
							))
							.ToList()
					))
					.ToListAsync(ct);

				return new PagedResponse<ActivResponse>(
					entity,
					activQuery.Page,
					activQuery.PageSize,
					total
				);
			},
			tags: Tags
		);
	}

	public async Task<Result<ActivResponse>> GetByIdAsync(int id, ActivScope scope)
	{
		var activ = await repo.QueryForScope(scope)
			.Where(a => a.ActivId == id)
			.Select(a => new ActivResponse(
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
				a.ActivDrugs.Select(a => new DrugResponse(
						a.DrugId,
						a.Drug.DrugName,
						a.Drug.DrugBrand,
						a.Drug.DrugForm
					))
					.ToList()
			))
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
		await cache.RemoveByTagAsync("activs");

		logger.LogInformation("Activity created: id={ActivId}, usr={UsrId}", activ.ActivId, usrId);
		return await GetByIdAsync(activ.ActivId, ActivScope.ForAll(usrId));
	}

	public async Task<Result<ActivResponse>> UpdateAsync(
		int id,
		UpdateActivRequest req,
		ActivScope scope
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
		await cache.RemoveByTagAsync("activs");
		logger.LogInformation("Activity updated: id={ActivId}", id);
		return await GetByIdAsync(id, ActivScope.ForAll(scope.CurrentUsrId));
	}

	public async Task<Result> DeleteAsync(int id, ActivScope scope)
	{
		var activ = await repo.QueryForScope(scope).FirstOrDefaultAsync(a => a.ActivId == id);
		if (activ is null)
			return Error.NotFound($"Активность {id} не найдена");

		activ.IsDeleted = true;
		await repo.UpdateAsync(activ);
		await cache.RemoveByTagAsync("activs");
		logger.LogInformation("Activity deleted: id={ActivId}", id);
		return Result.Success();
	}

	public async Task<Result> LinkDrugAsync(int activId, int drugId, ActivScope scope)
	{
		var exists = await repo.QueryForScope(scope).AnyAsync(a => a.ActivId == activId);
		if (!exists)
			return Error.NotFound($"Активность {activId} не найдена");

		await repo.LinkDrugAsync(activId, drugId);
		await cache.RemoveByTagAsync("activs");
		return Result.Success();
	}

	public async Task<Result> UnlinkDrugAsync(int activId, int drugId, ActivScope scope)
	{
		var exists = await repo.QueryForScope(scope).AnyAsync(a => a.ActivId == activId);
		if (!exists)
			return Error.NotFound($"Активность {activId} не найдена");

		var found = await repo.UnlinkDrugAsync(activId, drugId);
		if (!found)
			return Error.NotFound("Связь не найдена");
		await cache.RemoveByTagAsync("activs");
		return Result.Success();
	}
}
