namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;
using Trellis.Primitives;

public sealed record CreateCustomerCommand(
    FirstName FirstName,
    LastName LastName,
    EmailAddress Email,
    Maybe<PhoneNumber> PhoneNumber,
    ShippingAddress ShippingAddress) : ICommand<Result<Customer>>, IAuthorize
{
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.CustomersCreate];
}

public sealed class CreateCustomerCommandHandler : ICommandHandler<CreateCustomerCommand, Result<Customer>>
{
    private readonly ICustomerRepository _customers;
    private readonly TimeProvider _timeProvider;

    public CreateCustomerCommandHandler(ICustomerRepository customers, TimeProvider timeProvider)
    {
        _customers = customers;
        _timeProvider = timeProvider;
    }

    public async ValueTask<Result<Customer>> Handle(CreateCustomerCommand command, CancellationToken cancellationToken)
    {
        if (await _customers.ExistsByEmailAsync(command.Email, cancellationToken))
        {
            return Result.Fail<Customer>(new Error.Conflict(ResourceRef.For<Customer>(), "customers.duplicate_email")
            {
                Detail = "A customer with this email already exists.",
            });
        }

        var customer = new Customer(
            command.FirstName,
            command.LastName,
            command.Email,
            command.PhoneNumber,
            command.ShippingAddress,
            _timeProvider);
        _customers.Add(customer);
        return Result.Ok(customer);
    }
}
