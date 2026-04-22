using CrmWebApi.Common;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Department;
using CrmWebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmWebApi.Controllers;

[Route("api/departments")]
[Authorize(Roles = RoleNames.Admin)]
public class DepartmentsController(IDepartmentService service) : ApiController
{
	[HttpGet]
	[EndpointSummary("List departments")]
	[ProducesResponseType<PagedResponse<DepartmentResponse>>(StatusCodes.Status200OK)]
	public async Task<IActionResult> GetAll(
		[FromQuery] int page = 1,
		[FromQuery] int pageSize = 50,
		[FromQuery] bool includeTotal = true
	) =>
		FromResult(
			await service.GetAllAsync(Math.Max(page, 1), Math.Clamp(pageSize, 1, 200), includeTotal)
		);

	[HttpGet("{id:int}")]
	[EndpointSummary("Get department by ID")]
	[ProducesResponseType<DepartmentResponse>(StatusCodes.Status200OK)]
	public async Task<IActionResult> GetById(int id) => FromResult(await service.GetByIdAsync(id));

	[HttpPost]
	[EndpointSummary("Create department")]
	[ProducesResponseType<DepartmentResponse>(StatusCodes.Status201Created)]
	public async Task<IActionResult> Create([FromBody] CreateDepartmentRequest req)
	{
		var result = await service.CreateAsync(req);
		return CreatedResult(result, nameof(GetById), new { id = result.Value?.DepartmentId });
	}

	[HttpDelete("{id:int}")]
	[EndpointSummary("Delete department")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	public async Task<IActionResult> Delete(int id) => FromResult(await service.DeleteAsync(id));

	[HttpPost("{id:int}/users/{usrId:int}")]
	[EndpointSummary("Add user to department")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	public async Task<IActionResult> AddUser(int id, int usrId) =>
		FromResult(await service.AddUserAsync(id, usrId));

	[HttpDelete("{id:int}/users/{usrId:int}")]
	[EndpointSummary("Remove user from department")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	public async Task<IActionResult> RemoveUser(int id, int usrId) =>
		FromResult(await service.RemoveUserAsync(id, usrId));
}
