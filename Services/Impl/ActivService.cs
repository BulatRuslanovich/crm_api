using System.Globalization;
using CrmWebApi.Common;
using CrmWebApi.Data;
using CrmWebApi.Data.Entities;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Activ;
using CrmWebApi.Repositories;

namespace CrmWebApi.Services.Impl;

public class ActivService(
	IActivRepository repo,
	IAuditService audit,
	AppDbContext db,
	ICurrentUserService currentUser
) : IActivService
{
	public async Task<Result<PagedResponse<ActivResponse>>> GetAllAsync(ActivQuery activQuery)
	{
		if (currentUser.Scope is not { } scope)
			return Error.Forbidden("Доступ запрещён");

		return await repo.GetPagedForScopeAsync(activQuery, scope);
	}

	public async Task<Result<ActivResponse>> GetByIdAsync(int id)
	{
		if (currentUser.Scope is not { } scope)
			return Error.Forbidden("Доступ запрещён");

		return await GetByIdForScopeAsync(id, scope);
	}

	public async Task<Result<ActivResponse>> CreateAsync(CreateActivRequest req)
	{
		if (currentUser.UsrId is not { } usrId)
			return Error.Forbidden("Доступ запрещён");

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

		return await GetByIdForScopeAsync(added.ActivId, Scope.ForAll(usrId));
	}

	public async Task<Result<ActivResponse>> UpdateAsync(int id, UpdateActivRequest req)
	{
		if (currentUser.Scope is not { } scope)
			return Error.Forbidden("Доступ запрещён");

		var activ = await repo.GetForUpdateAsync(id, scope);
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
			return await GetByIdForScopeAsync(id, Scope.ForAll(scope.CurrentUsrId));

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
		return await GetByIdForScopeAsync(id, Scope.ForAll(scope.CurrentUsrId));
	}

	public async Task<Result> DeleteAsync(int id)
	{
		if (currentUser.Scope is not { } scope)
			return Error.Forbidden("Доступ запрещён");

		await using var tx = await db.Database.BeginTransactionAsync();
		var affected = await repo.SoftDeleteAsync(id, scope);
		if (affected == 0)
			return Error.NotFound($"Активность {id} не найдена");

		await audit.LogDeleteAsync(AuditEntityType.Activ, id);
		await tx.CommitAsync();
		return Result.Success();
	}

	public async Task<Result> LinkDrugAsync(int activId, int drugId)
	{
		if (currentUser.Scope is not { } scope)
			return Error.Forbidden("Доступ запрещён");

		if (!await repo.ExistsInScopeAsync(activId, scope))
			return Error.NotFound($"Активность {activId} не найдена");

		await repo.LinkDrugAsync(activId, drugId);
		return Result.Success();
	}

	public async Task<Result> UnlinkDrugAsync(int activId, int drugId)
	{
		if (currentUser.Scope is not { } scope)
			return Error.Forbidden("Доступ запрещён");

		if (!await repo.ExistsInScopeAsync(activId, scope))
			return Error.NotFound($"Активность {activId} не найдена");

		var found = await repo.UnlinkDrugAsync(activId, drugId);
		if (!found)
			return Error.NotFound("Связь не найдена");
		return Result.Success();
	}

	private async Task<Result<ActivResponse>> GetByIdForScopeAsync(int id, Scope scope)
	{
		var response = await repo.GetResponseByIdForScopeAsync(id, scope);
		return response is null ? Error.NotFound($"Активность {id} не найдена") : response;
	}
}
