using System.Security.Claims;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Activ;
using CrmWebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmWebApi.Controllers;

[Route("api/activs")]
[Tags("Activities")]
[Authorize]
public class ActivsController(IActivService service) : ApiController
{
	[HttpGet]
	[EndpointSummary("List activities")]
	[EndpointDescription(
		"Returns paginated activities. Admin/Director see all; Manager sees activities of users in the same department; Representative sees only their own."
	)]
	[ProducesResponseType<PagedResponse<ActivResponse>>(StatusCodes.Status200OK)]
	public async Task<IActionResult> GetAll([FromQuery] ActivQuery query)
	{
		if (!TryGetScope(out var scope, out var forbid))
			return forbid!;

		var maxPageSize = query.DateFrom != null ? 5000 : 500;
		var realPageSize = Math.Clamp(query.PageSize, 1, maxPageSize);

		query.Page = Math.Max(query.Page, 1);
		query.PageSize = realPageSize;

		return FromResult(await service.GetAllAsync(query, scope));
	}

	[HttpGet("{id:int}")]
	[EndpointSummary("Get activity by ID")]
	[EndpointDescription(
		"Returns a single activity. Admin/Director access any; Manager accesses activities of users in the same department; Representative accesses only their own."
	)]
	[ProducesResponseType<ActivResponse>(StatusCodes.Status200OK)]
	[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> GetById(int id)
	{
		if (!TryGetScope(out var scope, out var forbid))
			return forbid!;
		return FromResult(await service.GetByIdAsync(id, scope));
	}

	[HttpPost]
	[EndpointSummary("Create activity")]
	[EndpointDescription(
		"Creates a new activity linked to the authenticated user with specified drugs."
	)]
	[ProducesResponseType<ActivResponse>(StatusCodes.Status201Created)]
	public async Task<IActionResult> Create([FromBody] CreateActivRequest req)
	{
		var usrId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
		var result = await service.CreateAsync(usrId, req);
		return CreatedResult(result, nameof(GetById), new { id = result.Value?.ActivId });
	}

	[HttpPut("{id:int}")]
	[EndpointSummary("Update activity")]
	[EndpointDescription(
		"Updates activity fields. Admin/Director update any; Manager updates activities of users in the same department; Representative updates only their own. Null fields are not changed."
	)]
	[ProducesResponseType<ActivResponse>(StatusCodes.Status200OK)]
	[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> Update(int id, [FromBody] UpdateActivRequest req)
	{
		if (!TryGetScope(out var scope, out var forbid))
			return forbid!;
		return FromResult(await service.UpdateAsync(id, req, scope));
	}

	[HttpDelete("{id:int}")]
	[EndpointSummary("Delete activity")]
	[EndpointDescription(
		"Soft-deletes an activity. Admin/Director delete any; Manager deletes activities of users in the same department; Representative deletes only their own."
	)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> Delete(int id)
	{
		if (!TryGetScope(out var scope, out var forbid))
			return forbid!;
		return FromResult(await service.DeleteAsync(id, scope));
	}

	[HttpPost("{activId:int}/drugs/{drugId:int}")]
	[EndpointSummary("Link drug to activity")]
	[EndpointDescription(
		"Associates a drug with an activity. Admin/Director link on any; Manager on activities of users in the same department; Representative on only their own."
	)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> LinkDrug(int activId, int drugId)
	{
		if (!TryGetScope(out var scope, out var forbid))
			return forbid!;
		return FromResult(await service.LinkDrugAsync(activId, drugId, scope));
	}

	[HttpDelete("{activId:int}/drugs/{drugId:int}")]
	[EndpointSummary("Unlink drug from activity")]
	[EndpointDescription(
		"Removes a drug association from an activity. Admin/Director unlink on any; Manager on activities of users in the same department; Representative on only their own."
	)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> UnlinkDrug(int activId, int drugId)
	{
		if (!TryGetScope(out var scope, out var forbid))
			return forbid!;
		return FromResult(await service.UnlinkDrugAsync(activId, drugId, scope));
	}
}
