using Microsoft.EntityFrameworkCore;
using OrderManagement.Api.Application.Abstractions;
using OrderManagement.Api.Domain.Customers;
using OrderManagement.Api.Domain.Orders;
using OrderManagement.Api.Domain.Products;

namespace OrderManagement.Api.Infrastructure;

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

    public async Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default) =>
        await _db.Products.Where(p => ids.Contains(p.Id)).ToListAsync(ct);

    public Task<bool> ExistsBySkuAsync(string sku, CancellationToken ct = default) =>
        _db.Products.AnyAsync(p => p.Sku == sku, ct);

    public async Task AddAsync(Product product, CancellationToken ct = default) =>
        await _db.Products.AddAsync(product, ct);
}

public sealed class OrderRepository : IOrderRepository
{
    private readonly OrderManagementDbContext _db;
    public OrderRepository(OrderManagementDbContext db) => _db = db;

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Orders.FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<IReadOnlyList<Order>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default) =>
        await _db.Orders.Where(o => o.CustomerId == customerId).ToListAsync(ct);

    public async Task<IReadOnlyList<Order>> GetSubmittedBeforeAsync(DateTime submittedBeforeUtc, CancellationToken ct = default) =>
        await _db.Orders
            .Where(o => o.Status == OrderStatus.Submitted && o.SubmittedAt != null && o.SubmittedAt < submittedBeforeUtc)
            .ToListAsync(ct);

    public async Task AddAsync(Order order, CancellationToken ct = default) =>
        await _db.Orders.AddAsync(order, ct);

    public void TrackAddedLineItem(LineItem lineItem) =>
        _db.Entry(lineItem).State = EntityState.Added;
}

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly OrderManagementDbContext _db;
    public EfUnitOfWork(OrderManagementDbContext db) => _db = db;

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
