using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions;
using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Products;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Infrastructure.Repositories;

public sealed class CustomerRepository : ICustomerRepository
{
    private readonly OrderManagementDbContext _db;
    public CustomerRepository(OrderManagementDbContext db) => _db = db;

    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default) =>
        _db.Customers.AnyAsync(c => c.Email == email, ct);

    public async Task AddAsync(Customer customer, CancellationToken ct = default) =>
        await _db.Customers.AddAsync(customer, ct);
}

public sealed class ProductRepository : IProductRepository
{
    private readonly OrderManagementDbContext _db;
    public ProductRepository(OrderManagementDbContext db) => _db = db;

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<bool> ExistsBySkuAsync(string sku, CancellationToken ct = default) =>
        _db.Products.AnyAsync(p => p.Sku == sku, ct);

    public async Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var idList = ids.Distinct().ToList();
        return await _db.Products.Where(p => idList.Contains(p.Id)).ToListAsync(ct);
    }

    public async Task AddAsync(Product product, CancellationToken ct = default) =>
        await _db.Products.AddAsync(product, ct);
}

public sealed class OrderRepository : IOrderRepository
{
    private readonly OrderManagementDbContext _db;
    public OrderRepository(OrderManagementDbContext db) => _db = db;

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Orders.Include(o => o.LineItems).FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<IReadOnlyList<Order>> ListByCustomerAsync(Guid customerId, CancellationToken ct = default) =>
        await _db.Orders.Include(o => o.LineItems)
            .Where(o => o.CustomerId == customerId)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Order>> ListOverdueAsync(DateTimeOffset now, CancellationToken ct = default)
    {
        var threshold = OverdueOrderSpecification.SubmittedBefore(now);
        return await _db.Orders.Include(o => o.LineItems)
            .Where(o => o.Status == OrderStatus.Submitted
                        && o.SubmittedAt != null
                        && o.SubmittedAt < threshold)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Order order, CancellationToken ct = default) =>
        await _db.Orders.AddAsync(order, ct);
}

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly OrderManagementDbContext _db;
    public UnitOfWork(OrderManagementDbContext db) => _db = db;

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
