namespace OrderManagement.Application.Customers;

using Mediator;
using OrderManagement.Application.Abstractions;
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
    public IReadOnlyList<string> RequiredPermissions => [Permissions.CustomersCreate];
}

/// <summary>Handles <see cref="CreateCustomerCommand"/>.</summary>
public sealed class CreateCustomerCommandHandler(ICustomerRepository repository)
    : ICommandHandler<CreateCustomerCommand, Result<Customer>>
{
    /// <inheritdoc />
    public async ValueTask<Result<Customer>> Handle(CreateCustomerCommand command, CancellationToken cancellationToken)
    {
        if (await repository.ExistsByEmailAsync(command.Email, cancellationToken))
            return Result.Fail<Customer>(new Error.Conflict(
                ResourceRef.For<Customer>(command.Email.Value), "duplicate_email")
            {
                Detail = "A customer with this email already exists.",
            });

        var customer = new Customer(
            command.FirstName,
            command.LastName,
            command.Email,
            command.Phone,
            command.ShippingAddress);

        repository.Add(customer);
        return Result.Ok(customer);
    }
}
