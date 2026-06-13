namespace OrderManagement.Application.Customers;

using Mediator;
using OrderManagement.Domain;
using Trellis;
using Trellis.Authorization;

/// <summary>Creates a new customer.</summary>
public sealed record CreateCustomerCommand(
    FirstName FirstName,
    LastName LastName,
    Email Email,
    Maybe<PhoneNumber> PhoneNumber,
    ShippingAddress ShippingAddress) : ICommand<Result<Customer>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.CustomersCreate];
}

/// <summary>Handler for <see cref="CreateCustomerCommand"/>.</summary>
internal sealed class CreateCustomerCommandHandler(
    ICustomerRepository repository) : ICommandHandler<CreateCustomerCommand, Result<Customer>>
{
    public async ValueTask<Result<Customer>> Handle(CreateCustomerCommand command, CancellationToken cancellationToken)
    {
        if (await repository.ExistsByEmailAsync(command.Email, cancellationToken))
        {
            return Result.Fail<Customer>(
                new Error.Conflict(ResourceRef.For<Customer>(), "email.duplicate")
                {
                    Detail = "A customer with this email already exists.",
                });
        }

        var customer = new Customer(command.FirstName, command.LastName, command.Email, command.PhoneNumber, command.ShippingAddress);
        repository.Add(customer);
        return Result.Ok(customer);
    }
}
