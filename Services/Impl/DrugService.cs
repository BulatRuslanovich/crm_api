using CrmWebApi.Common;
using CrmWebApi.Data;
using CrmWebApi.Data.Entities;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Drug;
using Microsoft.EntityFrameworkCore;

namespace CrmWebApi.Services.Impl;

public class DrugService(AppDbContext db) : IDrugService
{
	public async Task<Result<PagedResponse<DrugResponse>>> GetAllAsync(
		int page,
		int pageSize,
		string? search = null,
		bool includeTotal = true
	)
	{
		var query = db.Drugs.Where(d => !d.IsDeleted).AsNoTracking();

		if (!string.IsNullOrEmpty(search))
		{
			var pattern = "%" + search + "%";

			query = query.Where(d =>
				EF.Functions.ILike(d.DrugName, pattern)
				|| EF.Functions.ILike(d.DrugBrand, pattern)
			);
		}

		var total = includeTotal ? await query.CountAsync() : 0;
		var entities = await query
			.OrderBy(d => d.DrugId)
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync();
		var items = entities.Select(DrugResponse.From).ToList();
		return new PagedResponse<DrugResponse>(items, page, pageSize, total);
	}

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
		return DrugResponse.From(drug);
	}

	public async Task<Result> DeleteAsync(int id)
	{
		var affected = await db.Drugs
			.Where(a => a.DrugId == id)
			 .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDeleted, true));

		return affected == 0
			? Error.NotFound($"Препарат {id} не найден")
			: Result.Success();
	}
}
