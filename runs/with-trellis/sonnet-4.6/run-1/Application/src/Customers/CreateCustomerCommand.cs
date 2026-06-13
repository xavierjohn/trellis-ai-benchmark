namespace OrderManagement.Application.Customers;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;
using Trellis.Primitives;

/// <summary>
/// Creates a customer.
/// </summary>
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

/// <summary>
/// Handles <see cref="CreateCustomerCommand"/>.
/// </summary>
public sealed class CreateCustomerCommandHandler(ICustomerRepository repository) : ICommandHandler<CreateCustomerCommand, Result<Customer>>
{
    /// <inheritdoc />
    public async ValueTask<Result<Customer>> Handle(CreateCustomerCommand command, CancellationToken cancellationToken)
    {
        var existing = await repository.FindByEmailAsync(command.Email, cancellationToken);
        if (existing.HasValue)
        {
            return Result.Fail<Customer>(new Error.Conflict(ResourceRef.For<Customer>(command.Email.Value), "customer.duplicate_email")
            {
                Detail = "A customer with this email already exists.",
            });
        }

        var customer = new Customer(command.FirstName, command.LastName, command.Email, command.PhoneNumber, command.ShippingAddress);
        repository.Add(customer);
        return Result.Ok(customer);
    }
}
