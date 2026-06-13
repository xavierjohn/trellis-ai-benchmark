using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Contracts;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Customers;

namespace OrderManagement.Application.Customers;

public sealed class CustomerService(ICustomerRepository customers, IUnitOfWork unitOfWork)
{
    public async Task<Result<CustomerResponse>> CreateAsync(Actor actor, CreateCustomerRequest request, CancellationToken ct = default)
    {
        if (!actor.Has(Permissions.CustomersCreate))
            return Error.Forbidden($"Actor '{actor.Id}' lacks permission '{Permissions.CustomersCreate}'.");

        var addressResult = ShippingAddress.Create(
            request.ShippingAddress?.Street,
            request.ShippingAddress?.City,
            request.ShippingAddress?.State,
            request.ShippingAddress?.PostalCode,
            request.ShippingAddress?.Country);
        if (addressResult.IsFailure)
            return addressResult.Error!;

        var customerResult = Customer.Create(
            request.FirstName, request.LastName, request.Email, request.PhoneNumber, addressResult.Value);
        if (customerResult.IsFailure)
            return customerResult.Error!;

        var customer = customerResult.Value;

        if (await customers.EmailExistsAsync(customer.Email, ct))
            return Error.Conflict($"A customer with email '{customer.Email}' already exists.");

        await customers.AddAsync(customer, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return customer.ToResponse();
    }

    public async Task<Result<CustomerResponse>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var customer = await customers.GetByIdAsync(id, ct);
        return customer is null
            ? Error.NotFound($"Customer '{id}' was not found.")
            : customer.ToResponse();
    }
}
