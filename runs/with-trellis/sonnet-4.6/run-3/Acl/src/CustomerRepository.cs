namespace OrderManagement.AntiCorruptionLayer;

using Microsoft.EntityFrameworkCore;
using OrderManagement.Application;
using OrderManagement.Domain;
using Trellis.EntityFrameworkCore;

internal class CustomerRepository(AppDbContext context) : RepositoryBase<Customer, CustomerId>(context), ICustomerRepository
{
    public Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken) =>
        DbSet.AnyAsync(customer => customer.Email == email, cancellationToken);
}
