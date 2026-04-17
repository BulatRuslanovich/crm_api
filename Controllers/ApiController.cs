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
	{
		var problem = new ProblemDetails
		{
			Status = StatusCodeFor(error.Type),
			Title = error.Message,
		};

		if (error.Extensions is not null)
			foreach (var (key, value) in error.Extensions)
				problem.Extensions[key] = value;

		return StatusCode(problem.Status!.Value, problem);
	}

	private static int StatusCodeFor(ErrorType type) =>
		type switch
		{
			ErrorType.NotFound => StatusCodes.Status404NotFound,
			ErrorType.Conflict => StatusCodes.Status409Conflict,
			ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
			ErrorType.Forbidden => StatusCodes.Status403Forbidden,
			ErrorType.Validation => StatusCodes.Status400BadRequest,
			_ => StatusCodes.Status500InternalServerError,
		};
}
