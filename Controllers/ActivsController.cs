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
		if (query.DateFrom != null && query.DateTo != null && query.DateFrom > query.DateTo)
		{
			(query.DateTo, query.DateFrom) = (query.DateFrom, query.DateTo);
		}

		var maxPageSize = query.DateFrom != null ? 5000 : 500;
		var realPageSize = Math.Clamp(query.PageSize, 1, maxPageSize);

		query.Page = Math.Max(query.Page, 1);
		query.PageSize = realPageSize;

		return FromResult(await service.GetAllAsync(query));
	}

	[HttpGet("{id:int}")]
	[EndpointSummary("Get activity by ID")]
	[ProducesResponseType<ActivResponse>(StatusCodes.Status200OK)]
	public async Task<IActionResult> GetById(int id) =>
		FromResult(await service.GetByIdAsync(id).ConfigureAwait(false));


	[HttpPost]
	[EndpointSummary("Create activity")]
	[ProducesResponseType<ActivResponse>(StatusCodes.Status201Created)]
	public async Task<IActionResult> Create([FromBody] CreateActivRequest req)
	{
		var result = await service.CreateAsync(req).ConfigureAwait(false);
		return CreatedResult(result, nameof(GetById), new { id = result.Value?.ActivId });
	}

	[HttpPut("{id:int}")]
	[EndpointSummary("Update activity")]
	[ProducesResponseType<ActivResponse>(StatusCodes.Status200OK)]
	public async Task<IActionResult> Update(int id, [FromBody] UpdateActivRequest req) =>
		FromResult(await service.UpdateAsync(id, req).ConfigureAwait(false));


	[HttpDelete("{id:int}")]
	[EndpointSummary("Delete activity")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	public async Task<IActionResult> Delete(int id) =>
		FromResult(await service.DeleteAsync(id).ConfigureAwait(false));


	[HttpPost("{activId:int}/drugs/{drugId:int}")]
	[EndpointSummary("Link drug to activity")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	public async Task<IActionResult> LinkDrug(int activId, int drugId) =>
		FromResult(await service.LinkDrugAsync(activId, drugId).ConfigureAwait(false));


	[HttpDelete("{activId:int}/drugs/{drugId:int}")]
	[EndpointSummary("Unlink drug from activity")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	public async Task<IActionResult> UnlinkDrug(int activId, int drugId) =>
		FromResult(await service.UnlinkDrugAsync(activId, drugId).ConfigureAwait(false));

}
