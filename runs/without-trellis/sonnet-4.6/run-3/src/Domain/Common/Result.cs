namespace Domain.Common;

public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public string? ErrorCode { get; }

    protected Result(bool isSuccess, string? error, string? errorCode)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorCode = errorCode;
    }

    public static Result Success() => new(true, null, null);
    public static Result Failure(string error, string errorCode = "validation") => new(false, error, errorCode);
    public static Result NotFound(string error) => new(false, error, "not_found");
    public static Result Conflict(string error) => new(false, error, "conflict");
    public static Result Forbidden(string error) => new(false, error, "forbidden");
    public static Result UnprocessableEntity(string error) => new(false, error, "unprocessable");
}

public class Result<T> : Result
{
    public T? Value { get; }

    private Result(bool isSuccess, T? value, string? error, string? errorCode)
        : base(isSuccess, error, errorCode)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(true, value, null, null);
    public new static Result<T> Failure(string error, string errorCode = "validation") => new(false, default, error, errorCode);
    public new static Result<T> NotFound(string error) => new(false, default, error, "not_found");
    public new static Result<T> Conflict(string error) => new(false, default, error, "conflict");
    public new static Result<T> Forbidden(string error) => new(false, default, error, "forbidden");
    public new static Result<T> UnprocessableEntity(string error) => new(false, default, error, "unprocessable");
}
