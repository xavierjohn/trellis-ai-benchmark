namespace OrderManagement.Domain.Common;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Forbidden
}

/// <summary>
/// Represents a domain or application failure. The <see cref="Type"/> determines
/// how the API layer maps the failure to an HTTP status code.
/// </summary>
public sealed record Error
{
    private Error(ErrorType type, string code, string message, IReadOnlyDictionary<string, string[]>? fieldErrors = null)
    {
        Type = type;
        Code = code;
        Message = message;
        FieldErrors = fieldErrors;
    }

    public ErrorType Type { get; }
    public string Code { get; }
    public string Message { get; }

    /// <summary>Per-field validation messages, present only for validation errors.</summary>
    public IReadOnlyDictionary<string, string[]>? FieldErrors { get; }

    public static Error Validation(string message, string code = "validation_error")
        => new(ErrorType.Validation, code, message);

    public static Error Validation(IReadOnlyDictionary<string, string[]> fieldErrors, string message = "One or more validation errors occurred.")
        => new(ErrorType.Validation, "validation_error", message, fieldErrors);

    public static Error NotFound(string message, string code = "not_found")
        => new(ErrorType.NotFound, code, message);

    public static Error Conflict(string message, string code = "conflict")
        => new(ErrorType.Conflict, code, message);

    public static Error Forbidden(string message, string code = "forbidden")
        => new(ErrorType.Forbidden, code, message);

    public override string ToString() => $"{Type}: {Message}";
}
