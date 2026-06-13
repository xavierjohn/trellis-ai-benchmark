namespace OrderManagement.Domain.Common;

public enum ErrorKind
{
    Validation,
    NotFound,
    Conflict,
    Forbidden
}

/// <summary>
/// A structured domain/application error. Maps to an HTTP status in the API layer.
/// </summary>
public sealed record Error(
    ErrorKind Kind,
    string Code,
    string Message,
    IReadOnlyDictionary<string, string[]>? Fields = null)
{
    public static Error Validation(string message, string code = "validation_error") =>
        new(ErrorKind.Validation, code, message);

    public static Error Validation(IReadOnlyDictionary<string, string[]> fields, string message = "One or more validation errors occurred.") =>
        new(ErrorKind.Validation, "validation_error", message, fields);

    public static Error NotFound(string message, string code = "not_found") =>
        new(ErrorKind.NotFound, code, message);

    public static Error Conflict(string message, string code = "conflict") =>
        new(ErrorKind.Conflict, code, message);

    public static Error Forbidden(string message, string code = "forbidden") =>
        new(ErrorKind.Forbidden, code, message);
}
