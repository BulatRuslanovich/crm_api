using CrmWebApi.Common;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Phys;
using CrmWebApi.DTOs.Spec;
using CrmWebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmWebApi.Controllers;

[Route("api/physes")]
[Tags("Contacts")]
[Authorize]
public class PhysesController(IPhysService physService) : ApiController
{
	[HttpGet("specs")]
	[EndpointSummary("List specialties")]
	[EndpointDescription("Returns all contact specialties. Cached for 10 minutes.")]
	[ProducesResponseType<IEnumerable<SpecResponse>>(StatusCodes.Status200OK)]
	public async Task<IActionResult> GetAllSpecs() =>
		FromResult(await physService.GetAllSpecsAsync());

	[HttpGet("specs/{id:int}")]
	[EndpointSummary("Get specialty by ID")]
	[ProducesResponseType<SpecResponse>(StatusCodes.Status200OK)]
	[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> GetSpecById(int id) =>
		FromResult(await physService.GetSpecByIdAsync(id));

	[HttpPost("specs")]
	[Authorize(Roles = RoleNames.Admin)]
	[EndpointSummary("Create specialty")]
	[ProducesResponseType<SpecResponse>(StatusCodes.Status201Created)]
	public async Task<IActionResult> CreateSpec([FromBody] CreateSpecRequest req)
	{
		var result = await physService.CreateSpecAsync(req);
		return CreatedResult(result, nameof(GetSpecById), new { id = result.Value?.SpecId });
	}

	[HttpDelete("specs/{id:int}")]
	[Authorize(Roles = RoleNames.Admin)]
	[EndpointSummary("Delete specialty")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> DeleteSpec(int id) =>
		FromResult(await physService.DeleteSpecAsync(id));

	[HttpGet]
	[EndpointSummary("List contacts")]
	[EndpointDescription(
		"Paginated list of contacts (physical persons) with specialty and linked organizations."
	)]
	[ProducesResponseType<PagedResponse<PhysResponse>>(StatusCodes.Status200OK)]
	public async Task<IActionResult> GetAll(
		[FromQuery] int page = 1,
		[FromQuery] int pageSize = 20,
		[FromQuery] string? search = null,
		[FromQuery] bool includeTotal = true
	) =>
		FromResult(
			await physService.GetAllAsync(
				Math.Max(page, 1),
				Math.Clamp(pageSize, 1, 100),
				search,
				includeTotal
			)
		);

	[HttpGet("{id:int}")]
	[EndpointSummary("Get contact by ID")]
	[ProducesResponseType<PhysResponse>(StatusCodes.Status200OK)]
	[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> GetById(int id) =>
		FromResult(await physService.GetByIdAsync(id));

	[HttpPost]
	[Authorize(Roles = RoleNames.Admin)]
	[EndpointSummary("Create contact")]
	[ProducesResponseType<PhysResponse>(StatusCodes.Status201Created)]
	public async Task<IActionResult> Create([FromBody] CreatePhysRequest req)
	{
		var result = await physService.CreateAsync(req);
		return CreatedResult(result, nameof(GetById), new { id = result.Value?.PhysId });
	}

	[HttpPut("{id:int}")]
	[Authorize(Roles = RoleNames.Admin)]
	[EndpointSummary("Update contact")]
	[EndpointDescription("Updates contact fields. Null fields are not changed. Admin only.")]
	[ProducesResponseType<PhysResponse>(StatusCodes.Status200OK)]
	[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> Update(int id, [FromBody] UpdatePhysRequest req) =>
		FromResult(await physService.UpdateAsync(id, req));

	[HttpDelete("{id:int}")]
	[Authorize(Roles = RoleNames.Admin)]
	[EndpointSummary("Delete contact")]
	[EndpointDescription("Soft-deletes a contact. Admin only.")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> Delete(int id) =>
		FromResult(await physService.DeleteAsync(id));

	[HttpPost("{physId:int}/orgs/{orgId:int}")]
	[Authorize(Roles = RoleNames.Admin)]
	[EndpointSummary("Link contact to organization")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	public async Task<IActionResult> LinkOrg(int physId, int orgId) =>
		FromResult(await physService.LinkOrgAsync(physId, orgId));

	[HttpDelete("{physId:int}/orgs/{orgId:int}")]
	[Authorize(Roles = RoleNames.Admin)]
	[EndpointSummary("Unlink contact from organization")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	public async Task<IActionResult> UnlinkOrg(int physId, int orgId) =>
		FromResult(await physService.UnlinkOrgAsync(physId, orgId));
}
