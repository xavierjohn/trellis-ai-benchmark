namespace OrderManagement.AntiCorruptionLayer;

using Microsoft.EntityFrameworkCore;
using OrderManagement.Application;
using OrderManagement.Domain;
using Trellis.EntityFrameworkCore;
using Trellis.Primitives;

/// <summary>EF customer repository.</summary>
internal sealed class CustomerRepository(AppDbContext context) : RepositoryBase<Customer, CustomerId>(context), ICustomerRepository
{
    public Task<Maybe<Customer>> FindByEmailAsync(EmailAddress email, CancellationToken cancellationToken) =>
        DbSet.Where(customer => customer.Email == email).FirstOrDefaultMaybeAsync(cancellationToken);
}

/// <summary>EF product repository.</summary>
internal sealed class ProductRepository(AppDbContext context) : RepositoryBase<Product, ProductId>(context), IProductRepository
{
    public Task<Maybe<Product>> FindBySkuAsync(Sku sku, CancellationToken cancellationToken) =>
        DbSet.Where(product => product.Sku == sku).FirstOrDefaultMaybeAsync(cancellationToken);

    public async Task<IReadOnlyList<Product>> FindByIdsAsync(IReadOnlyCollection<ProductId> ids, CancellationToken cancellationToken) =>
        await DbSet.Where(product => ids.Contains(product.Id)).ToListAsync(cancellationToken);
}

/// <summary>EF order repository.</summary>
internal sealed class OrderRepository(AppDbContext context) : RepositoryBase<Order, OrderId>(context), IOrderRepository
{
    public new Task<Maybe<Order>> FindByIdAsync(OrderId id, CancellationToken cancellationToken) =>
        DbSet
            .Include(order => order.LineItems)
            .Where(order => order.Id == id)
            .FirstOrDefaultMaybeAsync(cancellationToken);

    public async Task<IReadOnlyList<Order>> ListByCustomerAsync(CustomerId customerId, CancellationToken cancellationToken) =>
        await DbSet
            .Include(order => order.LineItems)
            .Where(order => order.CustomerId == customerId)
            .OrderBy(order => order.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Order>> ListOverdueAsync(DateTimeOffset asOf, CancellationToken cancellationToken)
    {
        var spec = new OverdueOrderSpecification(asOf);
        return await DbSet
            .Include(order => order.LineItems)
            .Where(spec)
            .OrderBy(order => order.SubmittedAt)
            .ToListAsync(cancellationToken);
    }
}
