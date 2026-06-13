namespace OrderManagement.Application;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;
using Trellis.Primitives;

/// <summary>Creates a new customer.</summary>
public sealed record CreateCustomerCommand(
    FirstName FirstName,
    LastName LastName,
    EmailAddress Email,
    Maybe<PhoneNumber> PhoneNumber,
    ShippingAddress ShippingAddress) : ICommand<Result<Customer>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions => [Permissions.CustomersCreate];
}

/// <summary>Handles <see cref="CreateCustomerCommand"/>.</summary>
public sealed class CreateCustomerCommandHandler(ICustomerRepository customers)
    : ICommandHandler<CreateCustomerCommand, Result<Customer>>
{
    /// <inheritdoc />
    public ValueTask<Result<Customer>> Handle(CreateCustomerCommand command, CancellationToken cancellationToken)
    {
        var customer = Customer.Create(
            command.FirstName,
            command.LastName,
            command.Email,
            command.PhoneNumber,
            command.ShippingAddress);

        customers.Add(customer);
        return Result.Ok(customer).AsValueTask();
    }
}
