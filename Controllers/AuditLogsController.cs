using CrmWebApi.Common;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Audit;
using CrmWebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmWebApi.Controllers;

[Route("api/audit-logs")]
[Authorize(Roles = RoleNames.Admin)]
public class AuditLogsController(IAuditService service) : ApiController
{
	[HttpGet]
	[EndpointSummary("List audit log entries")]
	[ProducesResponseType<PagedResponse<AuditLogResponse>>(StatusCodes.Status200OK)]
	public async Task<IActionResult> GetAll(
		[FromQuery] string? entityType = null,
		[FromQuery] int? entityId = null,
		[FromQuery] string? action = null,
		[FromQuery] int? changedBy = null,
		[FromQuery] DateTimeOffset? dateFrom = null,
		[FromQuery] DateTimeOffset? dateTo = null,
		[FromQuery] int page = 1,
		[FromQuery] int pageSize = 50,
		[FromQuery] bool includeTotal = true
	)
	{
		var query = new AuditLogQuery(entityType, entityId, action, changedBy, dateFrom, dateTo)
		{
			Page = Math.Max(page, 1),
			PageSize = Math.Clamp(pageSize, 1, 500),
			IncludeTotal = includeTotal,
		};
		return FromResult(await service.GetAllAsync(query));
	}

	[HttpGet("entity/{entityType}/{entityId:int}")]
	[EndpointSummary("List audit log for a specific entity")]
	[ProducesResponseType<PagedResponse<AuditLogResponse>>(StatusCodes.Status200OK)]
	public async Task<IActionResult> GetForEntity(
		string entityType,
		int entityId,
		[FromQuery] int page = 1,
		[FromQuery] int pageSize = 50,
		[FromQuery] bool includeTotal = true
	)
	{
		var query = new AuditLogQuery(entityType, entityId, null, null, null, null)
		{
			Page = Math.Max(page, 1),
			PageSize = Math.Clamp(pageSize, 1, 500),
			IncludeTotal = includeTotal,
		};
		return FromResult(await service.GetAllAsync(query));
	}
}
