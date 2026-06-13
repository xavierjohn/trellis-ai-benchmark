namespace OrderManagement.Api.Domain;

public class DomainValidationException : Exception
{
    public IReadOnlyList<string> Errors { get; }

    public DomainValidationException(string message) : base(message)
        => Errors = [message];

    public DomainValidationException(IEnumerable<string> errors)
        : base(string.Join("; ", errors))
        => Errors = errors.ToList();
}

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}

public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}
