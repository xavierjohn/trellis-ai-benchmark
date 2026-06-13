using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions;
using OrderManagement.Domain.Aggregates;
using OrderManagement.Domain.Enums;
using OrderManagement.Infrastructure.Data;

namespace OrderManagement.Infrastructure.Repositories;

public class CustomerRepository(OrderManagementDbContext db) : ICustomerRepository
{
    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Customer?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        db.Customers.FirstOrDefaultAsync(
            c => c.Email == email.Trim().ToLowerInvariant(), ct);

    public async Task AddAsync(Customer customer, CancellationToken ct = default) =>
        await db.Customers.AddAsync(customer, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}

public class ProductRepository(OrderManagementDbContext db) : IProductRepository
{
    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Product?> GetBySkuAsync(string sku, CancellationToken ct = default) =>
        db.Products.FirstOrDefaultAsync(
            p => p.SKU == sku.Trim().ToUpperInvariant(), ct);

    public async Task<IReadOnlyList<Product>> GetByIdsAsync(
        IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var idList = ids.ToList();
        return await db.Products
            .Where(p => idList.Contains(p.Id))
            .ToListAsync(ct);
    }

    public async Task AddAsync(Product product, CancellationToken ct = default) =>
        await db.Products.AddAsync(product, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}

public class OrderRepository(OrderManagementDbContext db) : IOrderRepository
{
    public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Orders
            .Include(o => o.LineItems)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<IReadOnlyList<Order>> GetByCustomerIdAsync(
        Guid customerId, CancellationToken ct = default) =>
        await db.Orders
            .Include(o => o.LineItems)
            .Where(o => o.CustomerId == customerId)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Order>> GetOverdueAsync(
        DateTimeOffset cutoff, CancellationToken ct = default)
    {
        // Load submitted orders first, then filter by date client-side
        // (avoids SQLite DateTimeOffset comparison translation issues)
        var submittedOrders = await db.Orders
            .Include(o => o.LineItems)
            .Where(o => o.Status == OrderStatus.Submitted)
            .ToListAsync(ct);

        return submittedOrders
            .Where(o => o.SubmittedAt.HasValue && o.SubmittedAt.Value <= cutoff)
            .ToList();
    }

    public async Task AddAsync(Order order, CancellationToken ct = default) =>
        await db.Orders.AddAsync(order, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
