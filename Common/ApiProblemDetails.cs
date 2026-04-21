using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CrmWebApi.Common;

public static class ApiProblemDetails
{
	public const string ContentType = "application/problem+json";
	public const string TraceIdExtension = "traceId";

	public static ProblemDetails FromError(Error error, HttpContext httpContext)
	{
		var statusCode = StatusCodeFor(error.Type);
		var problem = FromStatus(statusCode, error.Message, httpContext);

		if (error.Extensions is not null)
			foreach (var (key, value) in error.Extensions)
				problem.Extensions[key] = value;

		return problem;
	}

	public static ValidationProblemDetails FromValidationErrors(
		IDictionary<string, string[]> errors,
		HttpContext httpContext
	) => AddTraceId(
		new ValidationProblemDetails(errors)
		{
			Status = StatusCodes.Status400BadRequest,
			Title = "Ошибка валидации",
		},
		httpContext
	);

	public static ValidationProblemDetails FromModelState(
		ModelStateDictionary modelState,
		HttpContext httpContext
	) => AddTraceId(
		new ValidationProblemDetails(modelState)
		{
			Status = StatusCodes.Status400BadRequest,
			Title = "Ошибка валидации",
		},
		httpContext
	);

	public static ProblemDetails FromStatus(
		int statusCode,
		string? title,
		HttpContext httpContext,
		IReadOnlyDictionary<string, object?>? extensions = null
	)
	{
		var problem = AddTraceId(
			new ProblemDetails
			{
				Status = statusCode,
				Title = string.IsNullOrWhiteSpace(title)
					? DefaultTitleFor(statusCode)
					: title,
			},
			httpContext
		);

		if (extensions is not null)
			foreach (var (key, value) in extensions)
				problem.Extensions[key] = value;

		return problem;
	}

	public static ObjectResult ToActionResult(ProblemDetails problem)
	{
		var result = new ObjectResult(problem) { StatusCode = problem.Status };
		result.ContentTypes.Add(ContentType);
		return result;
	}

	public static async Task WriteAsync(
		HttpContext httpContext,
		ProblemDetails problem,
		CancellationToken cancellationToken = default
	)
	{
		httpContext.Response.StatusCode =
			problem.Status ?? StatusCodes.Status500InternalServerError;
		httpContext.Response.ContentType = ContentType;

		if (problem is ValidationProblemDetails validationProblem)
			await httpContext.Response.WriteAsJsonAsync(
				validationProblem,
				AppJsonContext.Default.ValidationProblemDetails,
				ContentType,
				cancellationToken
			);
		else
			await httpContext.Response.WriteAsJsonAsync(
				problem,
				AppJsonContext.Default.ProblemDetails,
				ContentType,
				cancellationToken
			);
	}

	public static int StatusCodeFor(ErrorType type) =>
		type switch
		{
			ErrorType.NotFound => StatusCodes.Status404NotFound,
			ErrorType.Conflict => StatusCodes.Status409Conflict,
			ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
			ErrorType.Forbidden => StatusCodes.Status403Forbidden,
			ErrorType.Validation => StatusCodes.Status400BadRequest,
			_ => StatusCodes.Status500InternalServerError,
		};

	public static string DefaultTitleFor(int statusCode) =>
		statusCode switch
		{
			StatusCodes.Status400BadRequest => "Некорректный запрос",
			StatusCodes.Status401Unauthorized => "Требуется авторизация",
			StatusCodes.Status403Forbidden => "Доступ запрещён",
			StatusCodes.Status404NotFound => "Ресурс не найден",
			StatusCodes.Status405MethodNotAllowed => "Метод не поддерживается",
			StatusCodes.Status409Conflict => "Конфликт данных",
			StatusCodes.Status429TooManyRequests => "Слишком много запросов",
			StatusCodes.Status500InternalServerError => "Внутренняя ошибка сервера",
			_ => "Ошибка запроса",
		};

	private static T AddTraceId<T>(T problem, HttpContext httpContext)
		where T : ProblemDetails
	{
		problem.Extensions[TraceIdExtension] = httpContext.TraceIdentifier;
		return problem;
	}
}
