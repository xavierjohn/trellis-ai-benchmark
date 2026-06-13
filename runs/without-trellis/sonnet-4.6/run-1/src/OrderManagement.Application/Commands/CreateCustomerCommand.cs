using MediatR;
using OrderManagement.Application.Abstractions;
using OrderManagement.Application.DTOs;
using OrderManagement.Application.Models;
using OrderManagement.Domain.Aggregates;
using OrderManagement.Domain.Exceptions;
using OrderManagement.Domain.ValueObjects;

namespace OrderManagement.Application.Commands;

public record CreateCustomerCommand(
    Actor Actor,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country) : IRequest<CustomerDto>;

public class CreateCustomerHandler(
    ICustomerRepository customerRepo) : IRequestHandler<CreateCustomerCommand, CustomerDto>
{
    public async Task<CustomerDto> Handle(CreateCustomerCommand req, CancellationToken ct)
    {
        if (!req.Actor.HasPermission(Permissions.CustomersCreate))
            throw new ForbiddenException("Permission 'customers:create' is required.");

        var existing = await customerRepo.GetByEmailAsync(req.Email, ct);
        if (existing is not null)
            throw new ConflictException($"A customer with email '{req.Email}' already exists.");

        var address = new ShippingAddress(req.Street, req.City, req.State, req.PostalCode, req.Country);
        var customer = Customer.Create(req.FirstName, req.LastName, req.Email, req.PhoneNumber, address);
        await customerRepo.AddAsync(customer, ct);
        await customerRepo.SaveChangesAsync(ct);
        return CustomerDto.FromCustomer(customer);
    }
}
