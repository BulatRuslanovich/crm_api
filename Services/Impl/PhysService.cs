using CrmWebApi.Common;
using CrmWebApi.Data;
using CrmWebApi.Data.Entities;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Phys;
using CrmWebApi.DTOs.Spec;
using CrmWebApi.Repositories;
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
	) => await repo.GetPagedAsync(page, pageSize, search, includeTotal);

	public async Task<Result<PhysResponse>> GetByIdAsync(int id)
	{
		var phys = await repo.GetResponseByIdAsync(id);
		return phys is null ? Error.NotFound($"Физическое лицо {id} не найдено") : phys;
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
		var old = await repo.GetAuditSnapshotAsync(id);
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
			await repo.UpdateAsync(id, req);
			await audit.LogUpdateAsync(AuditEntityType.Phys, id, diffs);
			await tx.CommitAsync();
		}

		return await GetByIdAsync(id);
	}

	public async Task<Result> DeleteAsync(int id)
	{
		await using var tx = await db.Database.BeginTransactionAsync();
		var affected = await repo.SoftDeleteAsync(id);
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
				async ct => (IEnumerable<SpecResponse>)await repo.GetSpecsAsync(ct),
				RefOptions,
				SpecTags
			)
		);

	public async Task<Result<SpecResponse>> GetSpecByIdAsync(int id)
	{
		var spec = await repo.GetSpecByIdAsync(id);
		return spec is null ? Error.NotFound($"Специальность {id} не найдена") : spec;
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
