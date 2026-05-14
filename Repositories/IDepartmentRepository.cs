using CrmWebApi.Data.Entities;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Department;

namespace CrmWebApi.Repositories;

public interface IDepartmentRepository
{
	public Task<PagedResponse<DepartmentResponse>> GetPagedAsync(
		int page,
		int pageSize,
		bool includeTotal
	);
	public Task<DepartmentResponse?> GetResponseByIdAsync(int id);
	public Task<Department> AddAsync(Department entity);
	public Task<int> SoftDeleteAsync(int id);
	public Task<bool> UserExistsAsync(int usrId);
	public Task<bool> IsUserLinkedAsync(int departmentId, int usrId);
	public Task LinkUserAsync(int departmentId, int usrId);
	public Task<bool> UnlinkUserAsync(int departmentId, int usrId);
}
