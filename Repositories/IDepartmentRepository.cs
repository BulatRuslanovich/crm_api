using CrmWebApi.Data.Entities;

namespace CrmWebApi.Repositories;

public interface IDepartmentRepository
{
	public IQueryable<Department> Query();
	public Task<Department?> FindAsync(int id);
	public Task<bool> ExistsByNameAsync(string name);
	public Task<Department> AddAsync(Department entity);
	public Task UpdateAsync(Department entity);
	public Task<bool> UserExistsAsync(int usrId);
	public Task<bool> IsUserLinkedAsync(int departmentId, int usrId);
	public Task LinkUserAsync(int departmentId, int usrId);
	public Task<bool> UnlinkUserAsync(int departmentId, int usrId);
}
