using CrmWebApi.Common;
using CrmWebApi.Data.Entities;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Department;
using CrmWebApi.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CrmWebApi.Services.Impl;

public class DepartmentService(IDepartmentRepository repo)
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
		var entity = new Department { DepartmentName = req.DepartmentName };
		var added = await repo.AddAsync(entity);
	
		return new DepartmentResponse(added.DepartmentId, added.DepartmentName, 0);
	}

	public async Task<Result> DeleteAsync(int id)
	{
		var affected = await repo.Query()
			.Where(a => a.DepartmentId == id)
			 .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDeleted, true));

		return affected == 0
			? Error.NotFound($"Департамент {id} не найден")
			: Result.Success();
	}

	public async Task<Result> AddUserAsync(int departmentId, int usrId)
	{
		await repo.LinkUserAsync(departmentId, usrId);
		return Result.Success();
	}

	public async Task<Result> RemoveUserAsync(int departmentId, int usrId)
	{
		var removed = await repo.UnlinkUserAsync(departmentId, usrId);
		if (!removed)
			return Error.NotFound("Пользователь не состоит в этом департаменте");

		return Result.Success();
	}
}
