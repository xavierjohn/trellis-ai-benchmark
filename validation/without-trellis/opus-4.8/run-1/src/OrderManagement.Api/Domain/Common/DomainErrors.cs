namespace OrderManagement.Api.Domain.Common;

/// <summary>
/// Base for all domain/application errors that map to a specific HTTP status.
/// </summary>
public abstract class AppException : Exception
{
    protected AppException(string message) : base(message) { }

    public abstract string ErrorCode { get; }
}

/// <summary>Semantic validation failure (well-formed request, invalid values / business rule). Maps to 422.</summary>
public sealed class ValidationAppException : AppException
{
    public ValidationAppException(string message, IReadOnlyDictionary<string, string[]>? errors = null)
        : base(message)
    {
        Errors = errors ?? new Dictionary<string, string[]>();
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public override string ErrorCode => "validation_error";
}

/// <summary>Entity not found by id. Maps to 404.</summary>
public sealed class NotFoundAppException : AppException
{
    public NotFoundAppException(string message) : base(message) { }

    public override string ErrorCode => "not_found";
}

/// <summary>Uniqueness / duplicate violation. Maps to 409.</summary>
public sealed class ConflictAppException : AppException
{
    public ConflictAppException(string message) : base(message) { }

    public override string ErrorCode => "conflict";
}

/// <summary>Missing permission / failed ownership check. Maps to 403.</summary>
public sealed class ForbiddenAppException : AppException
{
    public ForbiddenAppException(string message) : base(message) { }

    public override string ErrorCode => "forbidden";
}
