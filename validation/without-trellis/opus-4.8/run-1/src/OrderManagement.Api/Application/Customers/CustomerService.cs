using OrderManagement.Api.Application.Abstractions;
using OrderManagement.Api.Application.Auth;
using OrderManagement.Api.Contracts;
using OrderManagement.Api.Domain.Common;
using OrderManagement.Api.Domain.Customers;

namespace OrderManagement.Api.Application.Customers;

public sealed class CustomerService
{
    private readonly ICustomerRepository _customers;
    private readonly IUnitOfWork _uow;

    public CustomerService(ICustomerRepository customers, IUnitOfWork uow)
    {
        _customers = customers;
        _uow = uow;
    }

    public async Task<Customer> CreateAsync(Actor actor, CreateCustomerRequest request, CancellationToken ct = default)
    {
        actor.Require(Permissions.CustomersCreate);

        var addr = request.ShippingAddress;
        var address = new ShippingAddress(
            addr?.Street ?? string.Empty,
            addr?.City ?? string.Empty,
            addr?.State ?? string.Empty,
            addr?.PostalCode ?? string.Empty,
            addr?.Country ?? string.Empty);

        var customer = Customer.Create(
            request.FirstName ?? string.Empty,
            request.LastName ?? string.Empty,
            request.Email ?? string.Empty,
            request.PhoneNumber,
            address);

        if (await _customers.ExistsByEmailAsync(customer.Email, ct))
            throw new ConflictAppException($"A customer with email '{customer.Email}' already exists.");

        await _customers.AddAsync(customer, ct);
        await _uow.SaveChangesAsync(ct);
        return customer;
    }

    public async Task<Customer> GetRequiredAsync(Guid id, CancellationToken ct = default) =>
        await _customers.GetByIdAsync(id, ct)
        ?? throw new NotFoundAppException($"Customer '{id}' was not found.");
}
