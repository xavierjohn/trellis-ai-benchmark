namespace OrderManagement.AntiCorruptionLayer;

using Microsoft.EntityFrameworkCore;
using OrderManagement.Application;
using OrderManagement.Domain;
using Trellis.EntityFrameworkCore;
using Trellis.Primitives;

/// <summary>
/// Customer repository implementation.
/// </summary>
internal sealed class CustomerRepository(AppDbContext context) : RepositoryBase<Customer, CustomerId>(context), ICustomerRepository
{
    /// <inheritdoc />
    public Task<Maybe<Customer>> FindByEmailAsync(EmailAddress email, CancellationToken cancellationToken) =>
        DbSet.Where(customer => customer.Email == email).FirstOrDefaultMaybeAsync(cancellationToken);
}
