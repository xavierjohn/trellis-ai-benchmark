using System.Diagnostics.CodeAnalysis;

namespace OrderManagement.Domain.Common;

/// <summary>
/// Result of an operation that can succeed with a value or fail with an <see cref="Error"/>.
/// </summary>
public readonly struct Result<T>
{
    private readonly T? _value;

    private Result(T value)
    {
        _value = value;
        Error = null;
        IsSuccess = true;
    }

    private Result(Error error)
    {
        _value = default;
        Error = error;
        IsSuccess = false;
    }

    [MemberNotNullWhen(true, nameof(_value))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error? Error { get; }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value of a failed result.");

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(Error error) => new(error);

    public static implicit operator Result<T>(T value) => new(value);
    public static implicit operator Result<T>(Error error) => new(error);

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Error, TResult> onFailure) =>
        IsSuccess ? onSuccess(_value!) : onFailure(Error!);
}

/// <summary>
/// Result of an operation that can succeed (no value) or fail with an <see cref="Error"/>.
/// </summary>
public readonly struct Result
{
    private Result(Error? error)
    {
        Error = error;
        IsSuccess = error is null;
    }

    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error? Error { get; }

    public static Result Success() => new((Error?)null);
    public static Result Failure(Error error) => new(error);

    public static implicit operator Result(Error error) => new(error);
}
