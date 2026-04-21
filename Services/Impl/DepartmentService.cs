using CrmWebApi.Common;
using CrmWebApi.Data.Entities;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Department;
using CrmWebApi.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CrmWebApi.Services.Impl;

public class DepartmentService(IDepartmentRepository repo, ILogger<DepartmentService> logger)
	: IDepartmentService
{
	public async Task<Result<PagedResponse<DepartmentResponse>>> GetAllAsync(
		int page,
		int pageSize,
		bool includeTotal = true
	)
	{
		var query = repo.Query();
		var total = includeTotal ? await query.CountAsync() : 0;
		var items = await query
			.OrderBy(d => d.DepartmentId)
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.Select(d => new DepartmentResponse(
				d.DepartmentId,
				d.DepartmentName,
				d.UsrDepartments.Count
			))
			.ToListAsync();

		return new PagedResponse<DepartmentResponse>(items, page, pageSize, total);
	}

	public async Task<Result<DepartmentResponse>> GetByIdAsync(int id)
	{
		var department = await repo.Query()
			.Where(d => d.DepartmentId == id)
			.Select(d => new DepartmentResponse(
				d.DepartmentId,
				d.DepartmentName,
				d.UsrDepartments.Count
			))
			.FirstOrDefaultAsync();

		if (department is null)
			return Error.NotFound($"Департамент {id} не найден");

		return department;
	}

	public async Task<Result<DepartmentResponse>> CreateAsync(CreateDepartmentRequest req)
	{
		if (await repo.ExistsByNameAsync(req.DepartmentName))
			return Error.Conflict("Департамент с таким названием уже существует");

		var entity = new Department { DepartmentName = req.DepartmentName };
		await repo.AddAsync(entity);
		logger.LogInformation("Department created: id={DepartmentId}", entity.DepartmentId);
		return await GetByIdAsync(entity.DepartmentId);
	}

	public async Task<Result> DeleteAsync(int id)
	{
		var entity = await repo.FindAsync(id);
		if (entity is null)
			return Error.NotFound($"Департамент {id} не найден");

		entity.IsDeleted = true;
		await repo.UpdateAsync(entity);
		logger.LogInformation("Department deleted: id={DepartmentId}", id);
		return Result.Success();
	}

	public async Task<Result> AddUserAsync(int departmentId, int usrId)
	{
		var department = await repo.FindAsync(departmentId);
		if (department is null)
			return Error.NotFound($"Департамент {departmentId} не найден");

		if (!await repo.UserExistsAsync(usrId))
			return Error.NotFound($"Пользователь {usrId} не найден");

		if (await repo.IsUserLinkedAsync(departmentId, usrId))
			return Error.Conflict("Пользователь уже в этом департаменте");

		await repo.LinkUserAsync(departmentId, usrId);
		logger.LogInformation(
			"User {UsrId} linked to department {DepartmentId}",
			usrId,
			departmentId
		);
		return Result.Success();
	}

	public async Task<Result> RemoveUserAsync(int departmentId, int usrId)
	{
		var department = await repo.FindAsync(departmentId);
		if (department is null)
			return Error.NotFound($"Департамент {departmentId} не найден");

		var removed = await repo.UnlinkUserAsync(departmentId, usrId);
		if (!removed)
			return Error.NotFound("Пользователь не состоит в этом департаменте");

		logger.LogInformation(
			"User {UsrId} unlinked from department {DepartmentId}",
			usrId,
			departmentId
		);
		return Result.Success();
	}
}
