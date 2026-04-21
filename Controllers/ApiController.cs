using System.Security.Claims;
using CrmWebApi.Common;
using Microsoft.AspNetCore.Mvc;

namespace CrmWebApi.Controllers;

[ApiController]
public abstract class ApiController : ControllerBase
{
	protected IActionResult FromResult(Result result)
	{
		if (result.IsSuccess)
			return NoContent();
		return MapError(result.Error!);
	}

	protected IActionResult FromResult<T>(Result<T> result)
	{
		if (result.IsSuccess)
			return Ok(result.Value);
		return MapError(result.Error!);
	}

	protected IActionResult CreatedResult<T>(
		Result<T> result,
		string actionName,
		object routeValues
	)
	{
		if (result.IsSuccess)
			return CreatedAtAction(actionName, routeValues, result.Value);
		return MapError(result.Error!);
	}

	protected IActionResult MapError(Error error)
		=> ApiProblemDetails.ToActionResult(ApiProblemDetails.FromError(error, HttpContext));

	protected IActionResult ForbiddenProblem() =>
		MapError(Error.Forbidden("Доступ запрещён"));

	protected bool TryGetScope(out Scope scope, out IActionResult? forbid)
	{
		scope = default;
		forbid = null;

		var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (!int.TryParse(userIdClaim, out var usrId))
		{
			forbid = ForbiddenProblem();
			return false;
		}

		if (User.IsInRole(RoleNames.Admin) || User.IsInRole(RoleNames.Director))
			scope = Scope.ForAll(usrId);
		else if (User.IsInRole(RoleNames.Manager))
			scope = Scope.ForDepartment(usrId);
		else
			scope = Scope.ForOwn(usrId);

		return true;
	}
}
