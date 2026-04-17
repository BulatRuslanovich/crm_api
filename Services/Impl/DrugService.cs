using CrmWebApi.Common;
using CrmWebApi.Data;
using CrmWebApi.Data.Entities;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Drug;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace CrmWebApi.Services.Impl;

public class DrugService(AppDbContext db, HybridCache cache, ILogger<DrugService> logger)
	: IDrugService
{
	private static readonly string[] Tags = ["drugs"];

	public async Task<Result<PagedResponse<DrugResponse>>> GetAllAsync(
		int page,
		int pageSize,
		string? search = null
	) =>
		await cache.GetOrCreateAsync(
			$"drugs:{page}:{pageSize}:{search}",
			async ct =>
			{
				var query = db.Drugs.Where(d => !d.IsDeleted).AsNoTracking();

				if (!string.IsNullOrEmpty(search))
				{
					string pattern = "%" + search + "%";

					query = query.Where(d =>
						EF.Functions.ILike(d.DrugName, pattern)
						|| EF.Functions.ILike(d.DrugBrand, pattern)
					);
				}

				var total = await query.CountAsync(ct);
				var entities = await query
					.OrderBy(d => d.DrugId)
					.Skip((page - 1) * pageSize)
					.Take(pageSize)
					.ToListAsync(ct);
				var items = entities.Select(DrugResponse.From).ToList();
				return new PagedResponse<DrugResponse>(items, page, pageSize, total);
			},
			tags: Tags
		);

	public async Task<Result<DrugResponse>> GetByIdAsync(int id)
	{
		var drug = await db
			.Drugs.Where(d => !d.IsDeleted)
			.AsNoTracking()
			.FirstOrDefaultAsync(d => d.DrugId == id);
		if (drug is null)
			return Error.NotFound($"Препарат {id} не найден");
		return DrugResponse.From(drug);
	}

	public async Task<Result<DrugResponse>> CreateAsync(CreateDrugRequest req)
	{
		var drug = new Drug
		{
			DrugName = req.DrugName,
			DrugBrand = req.Brand,
			DrugForm = req.Form,
		};
		db.Drugs.Add(drug);
		await db.SaveChangesAsync();
		await cache.RemoveByTagAsync("drugs");
		logger.LogInformation("Drug created: {DrugName} (id={DrugId})", drug.DrugName, drug.DrugId);
		return DrugResponse.From(drug);
	}

	public async Task<Result<DrugResponse>> UpdateAsync(int id, UpdateDrugRequest req)
	{
		var drug = await db.Drugs.FirstOrDefaultAsync(d => d.DrugId == id && !d.IsDeleted);
		if (drug is null)
			return Error.NotFound($"Препарат {id} не найден");

		drug.DrugName = req.DrugName ?? drug.DrugName;
		drug.DrugBrand = req.Brand ?? drug.DrugBrand;
		drug.DrugForm = req.Form ?? drug.DrugForm;

		await db.SaveChangesAsync();
		await cache.RemoveByTagAsync("drugs");
		logger.LogInformation("Drug updated: id={DrugId}", id);
		return DrugResponse.From(drug);
	}

	public async Task<Result> DeleteAsync(int id)
	{
		var drug = await db.Drugs.FirstOrDefaultAsync(d => d.DrugId == id && !d.IsDeleted);
		if (drug is null)
			return Error.NotFound($"Препарат {id} не найден");

		drug.IsDeleted = true;
		await db.SaveChangesAsync();
		await cache.RemoveByTagAsync("drugs");
		logger.LogInformation("Drug deleted: id={DrugId}", id);
		return Result.Success();
	}
}
