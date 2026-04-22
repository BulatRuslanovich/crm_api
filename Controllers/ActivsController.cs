using System.Security.Claims;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Activ;
using CrmWebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmWebApi.Controllers;

[Route("api/activs")]
[Authorize]
public class ActivsController(IActivService service) : ApiController
{
	[HttpGet]
	[EndpointSummary("List activities")]
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
	[ProducesResponseType<ActivResponse>(StatusCodes.Status200OK)]
	public async Task<IActionResult> GetById(int id) =>
		!TryGetScope(out var scope, out var forbid) ? forbid! : FromResult(await service.GetByIdAsync(id, scope).ConfigureAwait(false));


	[HttpPost]
	[EndpointSummary("Create activity")]
	[ProducesResponseType<ActivResponse>(StatusCodes.Status201Created)]
	public async Task<IActionResult> Create([FromBody] CreateActivRequest req)
	{
		if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usrId))
		{
			return BadRequest();
		}

		var result = await service.CreateAsync(usrId, req).ConfigureAwait(false);
		return CreatedResult(result, nameof(GetById), new { id = result.Value?.ActivId });
	}

	[HttpPut("{id:int}")]
	[EndpointSummary("Update activity")]
	[ProducesResponseType<ActivResponse>(StatusCodes.Status200OK)]
	public async Task<IActionResult> Update(int id, [FromBody] UpdateActivRequest req) =>
		!TryGetScope(out var scope, out var forbid) ? forbid! : FromResult(await service.UpdateAsync(id, req, scope).ConfigureAwait(false));


	[HttpDelete("{id:int}")]
	[EndpointSummary("Delete activity")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	public async Task<IActionResult> Delete(int id) =>
		!TryGetScope(out var scope, out var forbid) ? forbid! : FromResult(await service.DeleteAsync(id, scope).ConfigureAwait(false));


	[HttpPost("{activId:int}/drugs/{drugId:int}")]
	[EndpointSummary("Link drug to activity")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	public async Task<IActionResult> LinkDrug(int activId, int drugId) =>
		!TryGetScope(out var scope, out var forbid) ? forbid! : FromResult(await service.LinkDrugAsync(activId, drugId, scope).ConfigureAwait(false));


	[HttpDelete("{activId:int}/drugs/{drugId:int}")]
	[EndpointSummary("Unlink drug from activity")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	public async Task<IActionResult> UnlinkDrug(int activId, int drugId) =>
		 !TryGetScope(out var scope, out var forbid) ? forbid! : FromResult(await service.UnlinkDrugAsync(activId, drugId, scope).ConfigureAwait((false)));

}
