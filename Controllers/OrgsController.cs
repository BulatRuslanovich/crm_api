using CrmWebApi.Common;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Org;
using CrmWebApi.DTOs.OrgType;
using CrmWebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmWebApi.Controllers;

[Route("api/orgs")]
[Authorize]
public class OrgsController(IOrgService service) : ApiController
{
	[HttpGet("types")]
	[EndpointSummary("List organization types")]
	[ProducesResponseType<IEnumerable<OrgTypeResponse>>(StatusCodes.Status200OK)]
	public async Task<IActionResult> GetAllTypes() => FromResult(await service.GetAllTypesAsync());

	[HttpGet]
	[EndpointSummary("List organizations")]
	[ProducesResponseType<PagedResponse<OrgResponse>>(StatusCodes.Status200OK)]
	public async Task<IActionResult> GetAll(
		[FromQuery] int page = 1,
		[FromQuery] int pageSize = 20,
		[FromQuery] string? search = null,
		[FromQuery] bool includeTotal = true
	) =>
		FromResult(
			await service.GetAllAsync(
				Math.Max(page, 1),
				Math.Clamp(pageSize, 1, 1000),
				search,
				includeTotal
			)
		);

	[HttpGet("{id:int}")]
	[EndpointSummary("Get organization by ID")]
	[ProducesResponseType<OrgResponse>(StatusCodes.Status200OK)]
	public async Task<IActionResult> GetById(int id) => FromResult(await service.GetByIdAsync(id));

	[HttpPost]
	[Authorize(Roles = RoleNames.Admin)]
	[EndpointSummary("Create organization")]
	[ProducesResponseType<OrgResponse>(StatusCodes.Status201Created)]
	public async Task<IActionResult> Create([FromBody] CreateOrgRequest req)
	{
		var result = await service.CreateAsync(req);
		return CreatedResult(result, nameof(GetById), new { id = result.Value?.OrgId });
	}

	[HttpPut("{id:int}")]
	[Authorize(Roles = RoleNames.Admin)]
	[EndpointSummary("Update organization")]
	[ProducesResponseType<OrgResponse>(StatusCodes.Status200OK)]
	public async Task<IActionResult> Update(int id, [FromBody] UpdateOrgRequest req) =>
		FromResult(await service.UpdateAsync(id, req));

	[HttpDelete("{id:int}")]
	[Authorize(Roles = RoleNames.Admin)]
	[EndpointSummary("Delete organization")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	public async Task<IActionResult> Delete(int id) => FromResult(await service.DeleteAsync(id));
}
