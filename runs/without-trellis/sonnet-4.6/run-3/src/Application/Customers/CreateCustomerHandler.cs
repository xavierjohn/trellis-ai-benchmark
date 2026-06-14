using Application.Common;
using Application.Interfaces;
using Domain.Common;
using Domain.Customers;

namespace Application.Customers;

public class CreateCustomerHandler
{
    private readonly ICustomerRepository _customerRepository;

    public CreateCustomerHandler(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<Result<Customer>> HandleAsync(CreateCustomerCommand command, Actor actor, CancellationToken ct = default)
    {
        if (!actor.HasPermission("customers:create"))
        {
            return Result<Customer>.Forbidden("You do not have permission to create customers.");
        }

        var addressResult = ShippingAddress.Create(command.Street, command.City, command.State, command.PostalCode, command.Country);
        if (!addressResult.IsSuccess)
        {
            return Result<Customer>.Failure(addressResult.Error!, addressResult.ErrorCode!);
        }

        var customerResult = Customer.Create(
            command.FirstName,
            command.LastName,
            command.Email,
            command.PhoneNumber,
            addressResult.Value!);

        if (!customerResult.IsSuccess)
        {
            return Result<Customer>.Failure(customerResult.Error!, customerResult.ErrorCode!);
        }

        var normalizedEmail = customerResult.Value!.Email;
        var emailExists = await _customerRepository.EmailExistsAsync(normalizedEmail, ct);
        if (emailExists)
        {
            return Result<Customer>.Conflict($"A customer with email '{command.Email}' already exists.");
        }

        await _customerRepository.AddAsync(customerResult.Value, ct);
        await _customerRepository.SaveChangesAsync(ct);
        return Result<Customer>.Success(customerResult.Value);
    }
}
