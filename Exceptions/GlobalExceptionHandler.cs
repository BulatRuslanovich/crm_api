using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CrmWebApi.Exceptions;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
	: IExceptionHandler
{
	public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
	{
		logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);

		ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
		await ctx.Response.WriteAsJsonAsync(
			new ProblemDetails
			{
				Status = StatusCodes.Status500InternalServerError,
				Title = "Внутренняя ошибка сервера",
				Extensions = { ["traceId"] = ctx.TraceIdentifier },
			},
			ct
		);

		return true;
	}
}
