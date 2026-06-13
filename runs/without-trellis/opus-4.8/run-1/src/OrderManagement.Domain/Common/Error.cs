namespace OrderManagement.Domain.Common;

/// <summary>
/// Categories of domain/application errors. Each maps to an HTTP status at the API edge.
/// </summary>
public enum ErrorKind
{
    Validation,
    NotFound,
    Conflict,
    Forbidden
}

/// <summary>
/// A structured, transport-agnostic error. Field-level details are optional and used for validation errors.
/// </summary>
public sealed record Error(
    ErrorKind Kind,
    string Code,
    string Message,
    IReadOnlyDictionary<string, string[]>? Fields = null)
{
    public static Error Validation(string message, IReadOnlyDictionary<string, string[]>? fields = null)
        => new(ErrorKind.Validation, "validation_error", message, fields);

    public static Error Validation(string field, string message)
        => new(ErrorKind.Validation, "validation_error", message,
            new Dictionary<string, string[]> { [field] = new[] { message } });

    public static Error NotFound(string message)
        => new(ErrorKind.NotFound, "not_found", message);

    public static Error Conflict(string message)
        => new(ErrorKind.Conflict, "conflict", message);

    public static Error Forbidden(string message)
        => new(ErrorKind.Forbidden, "forbidden", message);
}
