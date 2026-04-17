namespace CrmWebApi.Common;

public sealed record Error(
	string Message,
	ErrorType Type = ErrorType.Failure,
	IReadOnlyDictionary<string, object?>? Extensions = null
)
{
	public static Error Failure(string message) => new(message, ErrorType.Failure);

	public static Error NotFound(string message) => new(message, ErrorType.NotFound);

	public static Error Conflict(string message) => new(message, ErrorType.Conflict);

	public static Error Unauthorized(string message) => new(message, ErrorType.Unauthorized);

	public static Error Forbidden(
		string message,
		IReadOnlyDictionary<string, object?>? ext = null
	) => new(message, ErrorType.Forbidden, ext);

	public static Error Validation(string message) => new(message, ErrorType.Validation);
}
