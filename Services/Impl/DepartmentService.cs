using CrmWebApi.Common;
using CrmWebApi.Data.Entities;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Department;
using CrmWebApi.Repositories;

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
		return await repo.GetPagedAsync(page, pageSize, includeTotal);
	}

	public async Task<Result<DepartmentResponse>> GetByIdAsync(int id)
	{
		var department = await repo.GetResponseByIdAsync(id);

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
		var affected = await repo.SoftDeleteAsync(id);

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
