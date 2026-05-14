using CrmWebApi.Common;
using CrmWebApi.Data.Entities;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Department;
using CrmWebApi.Repositories;
using CrmWebApi.Services.Impl;

namespace CrmWebApi.Tests;

public sealed class DepartmentServiceTests
{
	private static DepartmentService CreateService(InMemoryDepartmentRepository repo) =>
		new(repo);

	[Fact]
	public async Task GetByIdAsync_ReturnsNotFound_WhenDepartmentMissing()
	{
		var repo = new InMemoryDepartmentRepository([]);
		var service = CreateService(repo);

		var result = await service.GetByIdAsync(999);

		Assert.False(result.IsSuccess);
		Assert.Equal(ErrorType.NotFound, result.Error!.Type);
	}

	[Fact]
	public async Task GetByIdAsync_ReturnsDepartment_WhenExists()
	{
		var dept = new Department { DepartmentName = "Продажи" };
		var repo = new InMemoryDepartmentRepository([dept]);
		var service = CreateService(repo);

		var result = await service.GetByIdAsync(dept.DepartmentId);

		Assert.True(result.IsSuccess);
		Assert.Equal("Продажи", result.Value!.DepartmentName);
	}

	[Fact]
	public async Task CreateAsync_CreatesDepartment()
	{
		var repo = new InMemoryDepartmentRepository([]);
		var service = CreateService(repo);

		var result = await service.CreateAsync(new CreateDepartmentRequest("Маркетинг"));

		Assert.True(result.IsSuccess);
		Assert.Equal("Маркетинг", result.Value!.DepartmentName);
	}


	[Fact]
	public async Task RemoveUserAsync_ReturnsNotFound_WhenUserNotInDepartment()
	{
		var dept = new Department { DepartmentName = "Отдел" };
		var repo = new InMemoryDepartmentRepository([dept], unlinkResult: false);
		var service = CreateService(repo);

		var result = await service.RemoveUserAsync(dept.DepartmentId, usrId: 1);

		Assert.False(result.IsSuccess);
		Assert.Equal(ErrorType.NotFound, result.Error!.Type);
	}

	private sealed class InMemoryDepartmentRepository(
		IEnumerable<Department> departments,
		bool userExists = true,
		bool userLinked = false,
		bool unlinkResult = true
	) : IDepartmentRepository
	{
		private readonly List<Department> _departments = departments.ToList();

		public Task<PagedResponse<DepartmentResponse>> GetPagedAsync(
			int page,
			int pageSize,
			bool includeTotal
		)
		{
			var departments = _departments
				.Where(d => !d.IsDeleted)
				.Select(ToResponse)
				.ToList();
			return Task.FromResult(new PagedResponse<DepartmentResponse>(
				departments.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
				page,
				pageSize,
				includeTotal ? departments.Count : 0
			));
		}

		public Task<DepartmentResponse?> GetResponseByIdAsync(int id) =>
			Task.FromResult(_departments
				.Where(d => d.DepartmentId == id && !d.IsDeleted)
				.Select(ToResponse)
				.FirstOrDefault());

		public Task<Department> AddAsync(Department entity)
		{
			_departments.Add(entity);
			return Task.FromResult(entity);
		}

		public Task<int> SoftDeleteAsync(int id)
		{
			var department = _departments.FirstOrDefault(d => d.DepartmentId == id && !d.IsDeleted);
			if (department is null) return Task.FromResult(0);
			department.IsDeleted = true;
			return Task.FromResult(1);
		}

		public Task<bool> UserExistsAsync(int usrId) => Task.FromResult(userExists);

		public Task<bool> IsUserLinkedAsync(int departmentId, int usrId) => Task.FromResult(userLinked);

		public Task LinkUserAsync(int departmentId, int usrId) => Task.CompletedTask;

		public Task<bool> UnlinkUserAsync(int departmentId, int usrId) => Task.FromResult(unlinkResult);

		private static DepartmentResponse ToResponse(Department department) =>
			new(department.DepartmentId, department.DepartmentName, department.UsrDepartments.Count);
	}
}
