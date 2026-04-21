using CrmWebApi.Data;
using CrmWebApi.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CrmWebApi.Repositories.Impl;

public class DepartmentRepository(AppDbContext db) : IDepartmentRepository
{
	public IQueryable<Department> Query() =>
		db.Departments.Where(d => !d.IsDeleted).AsNoTracking();

	public Task<Department?> FindAsync(int id) =>
		db.Departments.FirstOrDefaultAsync(d => d.DepartmentId == id && !d.IsDeleted);

	public Task<bool> ExistsByNameAsync(string name) =>
		db.Departments.AnyAsync(d => !d.IsDeleted && d.DepartmentName == name);

	public async Task<Department> AddAsync(Department entity)
	{
		db.Departments.Add(entity);
		await db.SaveChangesAsync();
		return entity;
	}

	public async Task UpdateAsync(Department entity)
	{
		if (db.Entry(entity).State == EntityState.Detached)
			db.Departments.Update(entity);
		await db.SaveChangesAsync();
	}

	public Task<bool> UserExistsAsync(int usrId) =>
		db.Usrs.AnyAsync(u => u.UsrId == usrId && !u.IsDeleted);

	public Task<bool> IsUserLinkedAsync(int departmentId, int usrId) =>
		db.UsrDepartments.AnyAsync(ud =>
			ud.DepartmentId == departmentId && ud.UsrId == usrId
		);

	public async Task LinkUserAsync(int departmentId, int usrId)
	{
		db.UsrDepartments.Add(new UsrDepartment { DepartmentId = departmentId, UsrId = usrId });
		await db.SaveChangesAsync();
	}

	public async Task<bool> UnlinkUserAsync(int departmentId, int usrId)
	{
		var deleted = await db
			.UsrDepartments.Where(ud => ud.DepartmentId == departmentId && ud.UsrId == usrId)
			.ExecuteDeleteAsync();
		return deleted > 0;
	}
}
