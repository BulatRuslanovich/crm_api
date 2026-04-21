using CrmWebApi.Common;
using Microsoft.AspNetCore.Diagnostics;

namespace CrmWebApi.Exceptions;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
	: IExceptionHandler
{
	public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
	{
		logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);

		await ApiProblemDetails.WriteAsync(
			ctx,
			ApiProblemDetails.FromStatus(
				StatusCodes.Status500InternalServerError,
				"Внутренняя ошибка сервера",
				ctx
			),
			ct
		);

		return true;
	}
}
