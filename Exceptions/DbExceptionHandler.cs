using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CrmWebApi.Exceptions;

public sealed class DbExceptionHandler : IExceptionHandler
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

		ctx.Response.StatusCode = status;
		await ctx.Response.WriteAsJsonAsync(
			new ProblemDetails
			{
				Status = status,
				Title = message,
				Extensions = { ["traceId"] = ctx.TraceIdentifier },
			},
			ct
		);

		return true;
	}
}
