namespace OrderManagement.AntiCorruptionLayer;

using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Orders;
using OrderManagement.Domain;
using Trellis.EntityFrameworkCore;
using Trellis.Primitives;

internal sealed class CustomerRepository : RepositoryBase<Customer, CustomerId>, ICustomerRepository
{
    public CustomerRepository(AppDbContext context) : base(context)
    {
    }

    public Task<bool> ExistsByEmailAsync(EmailAddress email, CancellationToken cancellationToken) =>
        DbSet.AnyAsync(c => c.Email == email, cancellationToken);
}

internal sealed class ProductRepository : RepositoryBase<Product, ProductId>, IProductRepository
{
    public ProductRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Product>> FindByIdsAsync(IReadOnlyCollection<ProductId> ids, CancellationToken cancellationToken) =>
        await DbSet.Where(p => ids.Contains(p.Id)).ToListAsync(cancellationToken);

    public Task<bool> ExistsBySkuAsync(Sku sku, CancellationToken cancellationToken) =>
        DbSet.AnyAsync(p => p.Sku == sku, cancellationToken);
}

internal sealed class OrderRepository : RepositoryBase<Order, OrderId>, IOrderRepository
{
    public OrderRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Order>> ListByCustomerAsync(CustomerId customerId, CancellationToken cancellationToken) =>
        await DbSet
            .AsNoTracking()
            .Where(o => o.CustomerId == customerId)
            .OrderBy(o => o.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Order>> ListOverdueAsync(DateTime asOfUtc, CancellationToken cancellationToken) =>
        await DbSet
            .AsNoTracking()
            .Where(new OverdueOrderSpecification(asOfUtc))
            .OrderByMaybe(o => o.SubmittedAt)
            .ToListAsync(cancellationToken);
}
