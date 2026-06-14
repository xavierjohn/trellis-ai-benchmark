namespace OrderManagement.AntiCorruptionLayer;

using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Customers;
using OrderManagement.Domain;
using Trellis.EntityFrameworkCore;
using Trellis.Primitives;

/// <summary>
/// EF Core customer repository.
/// </summary>
internal sealed class CustomerRepository(AppDbContext context) : ICustomerRepository
{
    public Task<Maybe<Customer>> FindByIdAsync(CustomerId id, CancellationToken cancellationToken) =>
        context.Customers.FirstOrDefaultMaybeAsync(customer => customer.Id == id, cancellationToken);

    public Task<Maybe<Customer>> FindByEmailAsync(EmailAddress email, CancellationToken cancellationToken) =>
        context.Customers.FirstOrDefaultMaybeAsync(customer => customer.Email == email, cancellationToken);

    public void Add(Customer customer) => context.Customers.Add(customer);
}
