using CrmWebApi.Common;
using CrmWebApi.Data.Entities;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Org;
using CrmWebApi.DTOs.OrgType;
using CrmWebApi.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace CrmWebApi.Services.Impl;

public class OrgService(IOrgRepository repo, HybridCache cache)
	: IOrgService
{
	private static readonly string[] TypeTags = ["org-types"];
	private static readonly HybridCacheEntryOptions RefOptions = new()
	{
		Expiration = TimeSpan.FromMinutes(10),
	};

	public async Task<Result<PagedResponse<OrgResponse>>> GetAllAsync(
		int page,
		int pageSize,
		string? search = null,
		bool includeTotal = true
	)
	{
		var query = repo.QueryHard();

		if (!string.IsNullOrEmpty(search))
		{
			var pattern = "%" + search + "%";

			query = query.Where(o =>
				EF.Functions.ILike(o.OrgName, pattern)
				|| EF.Functions.ILike(o.OrgAddress, pattern)
			);
		}

		var total = includeTotal ? await query.CountAsync() : 0;
		var items = await query
			.OrderBy(o => o.OrgId)
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.Select(o => new OrgResponse(
				o.OrgId,
				o.OrgTypeId,
				o.OrgType.OrgTypeName,
				o.OrgName,
				o.OrgInn,
				o.OrgLatitude,
				o.OrgLongitude,
				o.OrgAddress
			))
			.ToListAsync();
		return new PagedResponse<OrgResponse>(items, page, pageSize, total);
	}

	public async Task<Result<OrgResponse>> GetByIdAsync(int id)
	{
		var org = await repo.QueryHard()
			.Where(o => o.OrgId == id)
			.Select(o => new OrgResponse(
				o.OrgId,
				o.OrgTypeId,
				o.OrgType.OrgTypeName,
				o.OrgName,
				o.OrgInn,
				o.OrgLatitude,
				o.OrgLongitude,
				o.OrgAddress
			))
			.FirstOrDefaultAsync();
		if (org is null)
			return Error.NotFound($"Организация {id} не найдена");
		return org;
	}

	public async Task<Result<OrgResponse>> CreateAsync(CreateOrgRequest req)
	{
		var org = new Organization
		{
			OrgTypeId = req.OrgTypeId,
			OrgName = req.OrgName,
			OrgInn = req.Inn,
			OrgLatitude = req.Latitude,
			OrgLongitude = req.Longitude,
			OrgAddress = req.Address,
		};
		await repo.AddAsync(org);
		return await GetByIdAsync(org.OrgId);
	}

	public async Task<Result<OrgResponse>> UpdateAsync(int id, UpdateOrgRequest req)
	{
		var affected = await repo.QueryLite()
			.Where(o => o.OrgId == id)
			.ExecuteUpdateAsync(s => s
				.SetProperty(o => o.OrgTypeId, o => req.OrgTypeId ?? o.OrgTypeId)
				.SetProperty(o => o.OrgName, o => req.OrgName ?? o.OrgName)
				.SetProperty(o => o.OrgInn, o => req.Inn ?? o.OrgInn)
				.SetProperty(o => o.OrgLatitude, o => req.Latitude ?? o.OrgLatitude)
				.SetProperty(o => o.OrgLongitude, o => req.Longitude ?? o.OrgLongitude)
				.SetProperty(o => o.OrgAddress, o => req.Address ?? o.OrgAddress));

		if (affected == 0)
			return Error.NotFound($"Организация {id} не найдена");

		return await GetByIdAsync(id);
	}

	public async Task<Result> DeleteAsync(int id)
	{
		var affected = await repo.QueryLite()
			.Where(a => a.OrgId == id)
			 .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDeleted, true));

		return affected == 0
			? Error.NotFound($"Организация {id} не найдена")
			: Result.Success();
	}

	public async Task<Result<IEnumerable<OrgTypeResponse>>> GetAllTypesAsync() =>
		Result<IEnumerable<OrgTypeResponse>>.Success(
			await cache.GetOrCreateAsync(
				"org-types",
				async ct =>
					(IEnumerable<OrgTypeResponse>)
						await repo.QueryOrgTypes()
							.Select(ot => new OrgTypeResponse(ot.OrgTypeId, ot.OrgTypeName))
							.ToListAsync(ct),
				RefOptions,
				TypeTags
			)
		);
}
