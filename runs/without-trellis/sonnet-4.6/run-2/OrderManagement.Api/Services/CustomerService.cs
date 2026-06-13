using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Api.Auth;
using OrderManagement.Api.Domain;
using OrderManagement.Api.Infrastructure;
using OrderManagement.Api.Models;

namespace OrderManagement.Api.Services;

public class CustomerService
{
    private readonly AppDbContext _db;
    private readonly IValidator<CreateCustomerRequest> _validator;

    public CustomerService(AppDbContext db, IValidator<CreateCustomerRequest> validator)
    {
        _db = db;
        _validator = validator;
    }

    public async Task<Customer> CreateCustomer(Actor actor, CreateCustomerRequest request)
    {
        actor.RequirePermission(PermissionConstants.CustomersCreate);

        var validation = await _validator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new DomainValidationException(validation.Errors.Select(e => e.ErrorMessage));

        var exists = await _db.Customers.AnyAsync(c => c.Email == request.Email);
        if (exists)
            throw new ConflictException($"A customer with email '{request.Email}' already exists.");

        var address = new ShippingAddress(
            request.ShippingAddress.Street,
            request.ShippingAddress.City,
            request.ShippingAddress.State,
            request.ShippingAddress.PostalCode,
            request.ShippingAddress.Country);

        var customer = Customer.Create(
            request.FirstName, request.LastName, request.Email,
            request.PhoneNumber, address);

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        return customer;
    }
}
