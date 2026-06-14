namespace OrderManagement.Application.Customers;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;
using Trellis.Primitives;

/// <summary>Create customer command.</summary>
public sealed record CreateCustomerCommand(
    FirstName FirstName,
    LastName LastName,
    EmailAddress Email,
    Maybe<PhoneNumber> PhoneNumber,
    ShippingAddress ShippingAddress) : ICommand<Result<Customer>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.CustomersCreate];
}

/// <summary>Create customer handler.</summary>
public sealed class CreateCustomerCommandHandler(ICustomerRepository customers)
    : ICommandHandler<CreateCustomerCommand, Result<Customer>>
{
    /// <inheritdoc />
    public async ValueTask<Result<Customer>> Handle(CreateCustomerCommand command, CancellationToken cancellationToken)
    {
        var duplicate = await customers.FindByEmailAsync(command.Email, cancellationToken);
        if (duplicate.HasValue)
            return Result.Fail<Customer>(new Error.Conflict(ResourceRef.For<Customer>(), "customer.email.duplicate") { Detail = "A customer with this email already exists." });

        return Customer.TryCreate(command.FirstName, command.LastName, command.Email, command.PhoneNumber, command.ShippingAddress)
            .Tap(customers.Add);
    }
}
