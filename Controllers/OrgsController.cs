using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Org;
using CrmWebApi.DTOs.OrgType;
using CrmWebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmWebApi.Controllers;

[Route("api/orgs")]
[Tags("Organizations")]
[Authorize]
public class OrgsController(IOrgService service) : ApiController
{
	[HttpGet("types")]
	[EndpointSummary("List organization types")]
	[EndpointDescription("Returns all organization types. Cached for 10 minutes.")]
	[ProducesResponseType<IEnumerable<OrgTypeResponse>>(StatusCodes.Status200OK)]
	public async Task<IActionResult> GetAllTypes() => FromResult(await service.GetAllTypesAsync());

	[HttpGet]
	[EndpointSummary("List organizations")]
	[EndpointDescription("Paginated list of organizations with type info.")]
	[ProducesResponseType<PagedResponse<OrgResponse>>(StatusCodes.Status200OK)]
	public async Task<IActionResult> GetAll(
		[FromQuery] int page = 1,
		[FromQuery] int pageSize = 20,
		[FromQuery] string? search = null
	) =>
		FromResult(
			await service.GetAllAsync(Math.Max(page, 1), Math.Clamp(pageSize, 1, 100), search)
		);

	[HttpGet("{id:int}")]
	[EndpointSummary("Get organization by ID")]
	[ProducesResponseType<OrgResponse>(StatusCodes.Status200OK)]
	[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> GetById(int id) => FromResult(await service.GetByIdAsync(id));

	[HttpPost]
	[Authorize(Roles = "Admin")]
	[EndpointSummary("Create organization")]
	[EndpointDescription("Creates a new organization. Admin only.")]
	[ProducesResponseType<OrgResponse>(StatusCodes.Status201Created)]
	public async Task<IActionResult> Create([FromBody] CreateOrgRequest req)
	{
		var result = await service.CreateAsync(req);
		return CreatedResult(result, nameof(GetById), new { id = result.Value?.OrgId });
	}

	[HttpPut("{id:int}")]
	[Authorize(Roles = "Admin")]
	[EndpointSummary("Update organization")]
	[EndpointDescription("Updates organization fields. Null fields are not changed. Admin only.")]
	[ProducesResponseType<OrgResponse>(StatusCodes.Status200OK)]
	[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> Update(int id, [FromBody] UpdateOrgRequest req) =>
		FromResult(await service.UpdateAsync(id, req));

	[HttpDelete("{id:int}")]
	[Authorize(Roles = "Admin")]
	[EndpointSummary("Delete organization")]
	[EndpointDescription("Soft-deletes an organization. Admin only.")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> Delete(int id) => FromResult(await service.DeleteAsync(id));
}
