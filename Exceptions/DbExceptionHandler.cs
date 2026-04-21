using CrmWebApi.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CrmWebApi.Exceptions;

public sealed class DbExceptionHandler(ILogger<DbExceptionHandler> logger) : IExceptionHandler
{
	public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
	{
		if (ex is not DbUpdateException { InnerException: PostgresException pgEx })
			return false;

		var (status, message) = pgEx.SqlState switch
		{
			PostgresErrorCodes.UniqueViolation => (
				StatusCodes.Status409Conflict,
				"Запись с такими данными уже существует"
			),
			PostgresErrorCodes.ForeignKeyViolation => (
				StatusCodes.Status400BadRequest,
				"Связанная запись не найдена"
			),
			_ => (StatusCodes.Status500InternalServerError, "Ошибка базы данных"),
		};

		if (status >= StatusCodes.Status500InternalServerError)
			logger.LogError(ex, "Database exception: {SqlState}", pgEx.SqlState);

		await ApiProblemDetails.WriteAsync(
			ctx,
			ApiProblemDetails.FromStatus(status, message, ctx),
			ct
		);

		return true;
	}
}
