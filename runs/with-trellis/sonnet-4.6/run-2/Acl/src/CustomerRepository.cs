namespace OrderManagement.AntiCorruptionLayer;

using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Customers;
using OrderManagement.Domain;
using Trellis.EntityFrameworkCore;
using Trellis.Primitives;

/// <summary>EF Core implementation of ICustomerRepository.</summary>
internal class CustomerRepository : RepositoryBase<Customer, CustomerId>, ICustomerRepository
{
    private readonly AppDbContext _context;

    public CustomerRepository(AppDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<Maybe<Customer>> FindByEmailAsync(EmailAddress email, CancellationToken cancellationToken) =>
        await _context.Customers
            .Where(c => c.Email == email)
            .FirstOrDefaultMaybeAsync(cancellationToken);
}
