namespace OrderManagement.AntiCorruptionLayer.Repositories;

using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions;
using OrderManagement.Domain;
using Trellis.EntityFrameworkCore;
using Trellis.Primitives;

/// <summary>EF Core implementation of <see cref="ICustomerRepository"/>.</summary>
public sealed class CustomerRepository(AppDbContext context)
    : RepositoryBase<Customer, CustomerId>(context), ICustomerRepository
{
    /// <inheritdoc />
    public Task<bool> ExistsByEmailAsync(EmailAddress email, CancellationToken cancellationToken) =>
        DbSet.AnyAsync(c => c.Email == email, cancellationToken);
}
