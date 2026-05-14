using CrmWebApi.Data;
using CrmWebApi.Data.Entities;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Department;
using Microsoft.EntityFrameworkCore;

namespace CrmWebApi.Repositories.Impl;

public class DepartmentRepository(AppDbContext db) : IDepartmentRepository
{
	public async Task<PagedResponse<DepartmentResponse>> GetPagedAsync(
		int page,
		int pageSize,
		bool includeTotal
	)
	{
		var query = Query();
		var total = includeTotal ? await query.CountAsync() : 0;
		var items = await query
			.OrderBy(d => d.DepartmentId)
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.Select(d => ToResponse(d))
			.ToListAsync();

		return new PagedResponse<DepartmentResponse>(items, page, pageSize, total);
	}

	public Task<DepartmentResponse?> GetResponseByIdAsync(int id) =>
		Query()
			.Where(d => d.DepartmentId == id)
			.Select(d => ToResponse(d))
			.FirstOrDefaultAsync();

	public async Task<Department> AddAsync(Department entity)
	{
		db.Departments.Add(entity);
		await db.SaveChangesAsync();
		return entity;
	}

	public Task<int> SoftDeleteAsync(int id) =>
		db.Departments
			.Where(d => d.DepartmentId == id && !d.IsDeleted)
			.ExecuteUpdateAsync(s => s.SetProperty(d => d.IsDeleted, true));

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

	private IQueryable<Department> Query() =>
		db.Departments.Where(d => !d.IsDeleted).AsNoTracking();

	private static DepartmentResponse ToResponse(Department d) =>
		new(d.DepartmentId, d.DepartmentName, d.UsrDepartments.Count);
}
