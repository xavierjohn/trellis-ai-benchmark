using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Authorization;
using OrderManagement.Application.Contracts;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Customers;

namespace OrderManagement.Application.Customers;

public sealed class CustomerService
{
    private readonly ICustomerRepository _customers;
    private readonly IUnitOfWork _uow;
    private readonly IActorProvider _actorProvider;

    public CustomerService(ICustomerRepository customers, IUnitOfWork uow, IActorProvider actorProvider)
    {
        _customers = customers;
        _uow = uow;
        _actorProvider = actorProvider;
    }

    public async Task<Result<CustomerResponse>> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default)
    {
        var auth = AuthorizationGuard.Require(_actorProvider.Current, Permissions.CustomersCreate);
        if (auth.IsFailure) return auth.Error!;

        var addressInput = request.ShippingAddress ?? new ShippingAddressDto(null, null, null, null, null);
        var addressResult = ShippingAddress.Create(
            addressInput.Street, addressInput.City, addressInput.State, addressInput.PostalCode, addressInput.Country);
        if (addressResult.IsFailure) return addressResult.Error!;

        var customerResult = Customer.Create(
            request.FirstName, request.LastName, request.Email, request.PhoneNumber, addressResult.Value);
        if (customerResult.IsFailure) return customerResult.Error!;

        var customer = customerResult.Value;

        if (await _customers.ExistsByEmailAsync(customer.Email.Value, ct))
            return Error.Conflict($"A customer with email '{customer.Email.Value}' already exists.");

        await _customers.AddAsync(customer, ct);
        await _uow.SaveChangesAsync(ct);

        return CustomerResponse.From(customer);
    }
}
