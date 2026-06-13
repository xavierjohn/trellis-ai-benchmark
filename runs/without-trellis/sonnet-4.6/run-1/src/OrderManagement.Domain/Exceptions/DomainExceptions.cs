namespace OrderManagement.Domain.Exceptions;

public abstract class OrderManagementException : Exception
{
    protected OrderManagementException(string message) : base(message) { }
}

public class ValidationException : OrderManagementException
{
    public IReadOnlyList<string> Errors { get; }

    public ValidationException(string message) : base(message)
    {
        Errors = [message];
    }

    public ValidationException(IEnumerable<string> errors) : base("One or more validation errors occurred.")
    {
        Errors = errors.ToList();
    }
}

public class NotFoundException : OrderManagementException
{
    public NotFoundException(string entityName, object id)
        : base($"{entityName} with id '{id}' was not found.") { }

    public NotFoundException(string message) : base(message) { }
}

public class ConflictException : OrderManagementException
{
    public ConflictException(string message) : base(message) { }
}

public class ForbiddenException : OrderManagementException
{
    public ForbiddenException(string message) : base(message) { }
}
