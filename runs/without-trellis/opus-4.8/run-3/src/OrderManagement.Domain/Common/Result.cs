using System.Diagnostics.CodeAnalysis;

namespace OrderManagement.Domain.Common;

/// <summary>
/// A discriminated result type carrying either a success value or an <see cref="Error"/>.
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
        ? _value
        : throw new InvalidOperationException("Cannot access the value of a failed result.");

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(Error error) => new(error);

    public static implicit operator Result<T>(T value) => new(value);
    public static implicit operator Result<T>(Error error) => new(error);

    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure)
        => IsSuccess ? onSuccess(_value) : onFailure(Error);
}

/// <summary>Non-generic result used for operations that produce no value.</summary>
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
