using CrmWebApi.Common;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Department;

namespace CrmWebApi.Services;

public interface IDepartmentService
{
	public Task<Result<PagedResponse<DepartmentResponse>>> GetAllAsync(
		int page,
		int pageSize,
		bool includeTotal = true
	);
	public Task<Result<DepartmentResponse>> GetByIdAsync(int id);
	public Task<Result<DepartmentResponse>> CreateAsync(CreateDepartmentRequest req);
	public Task<Result> DeleteAsync(int id);
	public Task<Result> AddUserAsync(int departmentId, int usrId);
	public Task<Result> RemoveUserAsync(int departmentId, int usrId);
}
