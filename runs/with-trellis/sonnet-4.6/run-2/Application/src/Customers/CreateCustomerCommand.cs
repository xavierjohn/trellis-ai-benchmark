namespace OrderManagement.Application.Customers;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;
using Trellis.Primitives;

/// <summary>Creates a new customer.</summary>
public sealed record CreateCustomerCommand(
    FirstName FirstName,
    LastName LastName,
    EmailAddress Email,
    Maybe<PhoneNumber> Phone,
    ShippingAddress ShippingAddress) : ICommand<Result<Customer>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.CustomersCreate];
}

/// <summary>Handler for CreateCustomerCommand.</summary>
public sealed class CreateCustomerCommandHandler : ICommandHandler<CreateCustomerCommand, Result<Customer>>
{
    private readonly ICustomerRepository _repository;

    public CreateCustomerCommandHandler(ICustomerRepository repository)
    {
        _repository = repository;
    }

    public async ValueTask<Result<Customer>> Handle(CreateCustomerCommand command, CancellationToken cancellationToken)
    {
        var existing = await _repository.FindByEmailAsync(command.Email, cancellationToken);
        if (existing.HasValue)
            return Result.Fail<Customer>(
                new Error.Conflict(ResourceRef.For<Customer>(command.Email.Value), "customer.duplicate.email")
                { Detail = "A customer with this email already exists." });

        var customer = new Customer(command.FirstName, command.LastName, command.Email, command.Phone, command.ShippingAddress);
        _repository.Add(customer);
        return Result.Ok(customer);
    }
}
