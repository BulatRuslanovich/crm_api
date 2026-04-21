using CrmWebApi.Common;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Drug;
using CrmWebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmWebApi.Controllers;

[Route("api/drugs")]
[Tags("Drugs")]
[Authorize]
public class DrugsController(IDrugService service) : ApiController
{
	[HttpGet]
	[EndpointSummary("List drugs")]
	[EndpointDescription("Paginated list of pharmaceutical drugs.")]
	[ProducesResponseType<PagedResponse<DrugResponse>>(StatusCodes.Status200OK)]
	public async Task<IActionResult> GetAll(
		[FromQuery] int page = 1,
		[FromQuery] int pageSize = 20,
		[FromQuery] string? search = null,
		[FromQuery] bool includeTotal = true
	) =>
		FromResult(
			await service.GetAllAsync(
				Math.Max(page, 1),
				Math.Clamp(pageSize, 1, 100),
				search,
				includeTotal
			)
		);

	[HttpGet("{id:int}")]
	[EndpointSummary("Get drug by ID")]
	[ProducesResponseType<DrugResponse>(StatusCodes.Status200OK)]
	[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> GetById(int id) => FromResult(await service.GetByIdAsync(id));

	[HttpPost]
	[Authorize(Roles = RoleNames.Admin)]
	[EndpointSummary("Create drug")]
	[ProducesResponseType<DrugResponse>(StatusCodes.Status201Created)]
	public async Task<IActionResult> Create([FromBody] CreateDrugRequest req)
	{
		var result = await service.CreateAsync(req);
		return CreatedResult(result, nameof(GetById), new { id = result.Value?.DrugId });
	}

	[HttpPut("{id:int}")]
	[Authorize(Roles = RoleNames.Admin)]
	[EndpointSummary("Update drug")]
	[EndpointDescription("Updates drug fields. Null fields are not changed. Admin only.")]
	[ProducesResponseType<DrugResponse>(StatusCodes.Status200OK)]
	[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> Update(int id, [FromBody] UpdateDrugRequest req) =>
		FromResult(await service.UpdateAsync(id, req));

	[HttpDelete("{id:int}")]
	[Authorize(Roles = RoleNames.Admin)]
	[EndpointSummary("Delete drug")]
	[EndpointDescription("Soft-deletes a drug. Admin only.")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> Delete(int id) => FromResult(await service.DeleteAsync(id));
}
