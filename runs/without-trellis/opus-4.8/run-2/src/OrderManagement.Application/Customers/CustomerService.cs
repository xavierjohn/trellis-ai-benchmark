using OrderManagement.Application.Abstractions;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Customers;

namespace OrderManagement.Application.Customers;

public sealed class CustomerService
{
    private readonly ICustomerRepository _customers;
    private readonly IUnitOfWork _unitOfWork;

    public CustomerService(ICustomerRepository customers, IUnitOfWork unitOfWork)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CustomerDto>> CreateAsync(IActor actor, CreateCustomerRequest request, CancellationToken ct = default)
    {
        if (!actor.Has(Permissions.CustomersCreate))
            return Error.Forbidden($"Actor lacks required permission '{Permissions.CustomersCreate}'.");

        var addr = request.ShippingAddress;
        var addressResult = ShippingAddress.Create(
            addr?.Street, addr?.City, addr?.State, addr?.PostalCode, addr?.Country);
        if (addressResult.IsFailure)
            return addressResult.Error!;

        var customerResult = Customer.Create(
            request.FirstName, request.LastName, request.Email, request.PhoneNumber, addressResult.Value);
        if (customerResult.IsFailure)
            return customerResult.Error!;

        var customer = customerResult.Value;

        if (await _customers.ExistsByEmailAsync(customer.Email, ct))
            return Error.Conflict($"A customer with email '{customer.Email}' already exists.");

        await _customers.AddAsync(customer, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return customer.ToDto();
    }
}
