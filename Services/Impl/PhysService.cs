using CrmWebApi.Common;
using CrmWebApi.Data;
using CrmWebApi.Data.Entities;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Org;
using CrmWebApi.DTOs.Phys;
using CrmWebApi.DTOs.Spec;
using CrmWebApi.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace CrmWebApi.Services.Impl;

public class PhysService(IPhysRepository repo, HybridCache cache, IAuditService audit, AppDbContext db)
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
						null,
						null,
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
						null,
						null,
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

		await using var tx = await db.Database.BeginTransactionAsync();
		await repo.AddAsync(phys);
		await audit.LogCreateAsync(AuditEntityType.Phys, phys.PhysId);
		await tx.CommitAsync();

		return await GetByIdAsync(phys.PhysId);
	}

	public async Task<Result<PhysResponse>> UpdateAsync(int id, UpdatePhysRequest req)
	{
		var old = await repo.QueryLite()
			.Where(p => p.PhysId == id)
			.Select(p => new
			{
				p.SpecId,
				p.PhysFirstname,
				p.PhysLastname,
				p.PhysMiddlename,
				p.PhysPhone,
				p.PhysEmail,
			})
			.FirstOrDefaultAsync();

		if (old is null)
			return Error.NotFound($"Физическое лицо {id} не найдено");

		var diffs = new List<AuditDiff>();
		if (req.SpecId is { } sid && sid != old.SpecId)
			diffs.Add(new("spec_id", old.SpecId.ToString(), sid.ToString()));
		if (req.FirstName is { } fn && fn != old.PhysFirstname)
			diffs.Add(new("phys_firstname", old.PhysFirstname, fn));
		if (req.LastName is { } ln && ln != old.PhysLastname)
			diffs.Add(new("phys_lastname", old.PhysLastname, ln));
		if (req.MiddleName is { } mn && mn != old.PhysMiddlename)
			diffs.Add(new("phys_middlename", old.PhysMiddlename, mn));
		if (req.Phone is { } ph && ph != old.PhysPhone)
			diffs.Add(new("phys_phone", old.PhysPhone, ph));
		if (req.Email is { } em && em != old.PhysEmail)
			diffs.Add(new("phys_email", old.PhysEmail, em));

		if (diffs.Count > 0)
		{
			await using var tx = await db.Database.BeginTransactionAsync();
			await repo.QueryLite()
				.Where(p => p.PhysId == id)
				.ExecuteUpdateAsync(s => s
					.SetProperty(p => p.SpecId, p => req.SpecId ?? p.SpecId)
					.SetProperty(p => p.PhysFirstname, p => req.FirstName ?? p.PhysFirstname)
					.SetProperty(p => p.PhysLastname, p => req.LastName ?? p.PhysLastname)
					.SetProperty(p => p.PhysMiddlename, p => req.MiddleName ?? p.PhysMiddlename)
					.SetProperty(p => p.PhysPhone, p => req.Phone ?? p.PhysPhone)
					.SetProperty(p => p.PhysEmail, p => req.Email ?? p.PhysEmail));

			await audit.LogUpdateAsync(AuditEntityType.Phys, id, diffs);
			await tx.CommitAsync();
		}

		return await GetByIdAsync(id);
	}

	public async Task<Result> DeleteAsync(int id)
	{
		await using var tx = await db.Database.BeginTransactionAsync();
		var affected = await repo.QueryLite()
		.Where(a => a.PhysId == id)
		 .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDeleted, true));

		if (affected == 0)
			return Error.NotFound($"Физическое лицо {id} не найдено");

		await audit.LogDeleteAsync(AuditEntityType.Phys, id);
		await tx.CommitAsync();
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
