namespace CrmWebApi.Common;

public sealed class Result
{
	public bool IsSuccess { get; }
	public Error? Error { get; }

	private Result() => IsSuccess = true;

	private Result(Error error)
	{
		Error = error;
	}

	public static Result Success() => new();

	private static Result Failure(Error error) => new(error);

	public static implicit operator Result(Error error) => Failure(error);
}

public sealed class Result<T>
{
	public bool IsSuccess { get; }
	public T? Value { get; }
	public Error? Error { get; }

	private Result(T value)
	{
		IsSuccess = true;
		Value = value;
	}

	private Result(Error error)
	{
		Error = error;
	}

	public static Result<T> Success(T value) => new(value);

	private static Result<T> Failure(Error error) => new(error);

	public static implicit operator Result<T>(Error error) => Failure(error);

	public static implicit operator Result<T>(T value) => Success(value);
}
