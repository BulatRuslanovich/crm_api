using CrmWebApi.Common;
using CrmWebApi.Data.Entities;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Org;
using CrmWebApi.DTOs.Phys;
using CrmWebApi.DTOs.Spec;
using CrmWebApi.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace CrmWebApi.Services.Impl;

public class PhysService(IPhysRepository repo, HybridCache cache)
	: IPhysService
{
	private static readonly string[] SpecTags = ["specs"];
	private static readonly HybridCacheEntryOptions RefOptions = new()
	{
		Expiration = TimeSpan.FromMinutes(10),
	};

	public async Task<Result<PagedResponse<PhysResponse>>> GetAllAsync(
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
			query = query.Where(p =>
				EF.Functions.ILike(p.PhysFirstname, pattern)
				|| EF.Functions.ILike(p.PhysLastname, pattern)
				|| EF.Functions.ILike(p.PhysMiddlename ?? "", pattern)
			);
		}

		var total = includeTotal ? await query.CountAsync() : 0;
		var items = await query
			.OrderBy(p => p.PhysId)
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.Select(p => new PhysResponse(
				p.PhysId,
				p.SpecId,
				p.Spec.SpecName,
				p.PhysFirstname,
				p.PhysLastname,
				p.PhysMiddlename,
				p.PhysPhone,
				p.PhysEmail,
				p.PhysOrgs.Select(po => new OrgResponse(
						po.OrgId,
						0,
						"-",
						po.Org.OrgName,
						"-",
						0,
						0,
						"-"
					))
					.ToList()
			))
			.ToListAsync();

		return new PagedResponse<PhysResponse>(items, page, pageSize, total);
	}

	public async Task<Result<PhysResponse>> GetByIdAsync(int id)
	{
		var phys = await repo.QueryHard()
			.Where(p => p.PhysId == id)
			.Select(p => new PhysResponse(
				p.PhysId,
				p.SpecId,
				p.Spec.SpecName,
				p.PhysFirstname,
				p.PhysLastname,
				p.PhysMiddlename,
				p.PhysPhone,
				p.PhysEmail,
				p.PhysOrgs.Select(po => new OrgResponse(
						po.OrgId,
						0,
						"-",
						po.Org.OrgName,
						"-",
						0,
						0,
						"-"
					))
					.ToList()
			))
			.FirstOrDefaultAsync();
		if (phys is null)
			return Error.NotFound($"Физическое лицо {id} не найдено");
		return phys;
	}

	public async Task<Result<PhysResponse>> CreateAsync(CreatePhysRequest req)
	{
		var phys = new Phys
		{
			SpecId = req.SpecId,
			PhysFirstname = req.FirstName,
			PhysLastname = req.LastName,
			PhysMiddlename = req.MiddleName,
			PhysPhone = req.Phone,
			PhysEmail = req.Email,
		};

		await repo.AddAsync(phys);
		return await GetByIdAsync(phys.PhysId);
	}

	public async Task<Result<PhysResponse>> UpdateAsync(int id, UpdatePhysRequest req)
	{
		var phys = await repo.QueryLite().FirstOrDefaultAsync(p => p.PhysId == id);
		if (phys is null)
			return Error.NotFound($"Физическое лицо {id} не найдено");

		phys.SpecId = req.SpecId ?? phys.SpecId;
		phys.PhysFirstname = req.FirstName ?? phys.PhysFirstname;
		phys.PhysLastname = req.LastName ?? phys.PhysLastname;
		phys.PhysMiddlename = req.MiddleName ?? phys.PhysMiddlename;
		phys.PhysPhone = req.Phone ?? phys.PhysPhone;
		phys.PhysEmail = req.Email ?? phys.PhysEmail;

		await repo.UpdateAsync(phys);
		return await GetByIdAsync(id);
	}

	public async Task<Result> DeleteAsync(int id)
	{
		var phys = await repo.QueryLite().FirstOrDefaultAsync(p => p.PhysId == id);
		if (phys is null)
			return Error.NotFound($"Физическое лицо {id} не найдено");

		phys.IsDeleted = true;
		await repo.UpdateAsync(phys);
		return Result.Success();
	}

	public async Task<Result> LinkOrgAsync(int physId, int orgId)
	{
		await repo.LinkOrgAsync(physId, orgId);
		return Result.Success();
	}

	public async Task<Result> UnlinkOrgAsync(int physId, int orgId)
	{
		var found = await repo.UnlinkOrgAsync(physId, orgId);
		if (!found)
			return Error.NotFound("Связь не найдена");
		return Result.Success();
	}

	public async Task<Result<IEnumerable<SpecResponse>>> GetAllSpecsAsync() =>
		Result<IEnumerable<SpecResponse>>.Success(
			await cache.GetOrCreateAsync(
				"specs",
				async ct =>
					(IEnumerable<SpecResponse>)
						await repo.QuerySpecs()
							.Select(s => new SpecResponse(s.SpecId, s.SpecName))
							.ToListAsync(ct),
				RefOptions,
				SpecTags
			)
		);

	public async Task<Result<SpecResponse>> GetSpecByIdAsync(int id)
	{
		var spec = await repo.QuerySpecs().FirstOrDefaultAsync(s => s.SpecId == id);
		if (spec is null)
			return Error.NotFound($"Специальность {id} не найдена");
		return new SpecResponse(spec.SpecId, spec.SpecName);
	}

	public async Task<Result<SpecResponse>> CreateSpecAsync(CreateSpecRequest req)
	{
		var spec = await repo.AddSpecAsync(new Spec { SpecName = req.SpecName });
		await cache.RemoveByTagAsync("specs");
		return new SpecResponse(spec.SpecId, spec.SpecName);
	}

	public async Task<Result> DeleteSpecAsync(int id)
	{
		var found = await repo.SoftDeleteSpecAsync(id);
		if (!found)
			return Error.NotFound($"Специальность {id} не найдена");
		await cache.RemoveByTagAsync("specs");
		return Result.Success();
	}
}
