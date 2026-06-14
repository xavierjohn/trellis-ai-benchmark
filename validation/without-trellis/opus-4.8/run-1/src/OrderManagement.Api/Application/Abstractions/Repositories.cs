using OrderManagement.Api.Domain.Customers;
using OrderManagement.Api.Domain.Orders;
using OrderManagement.Api.Domain.Products;

namespace OrderManagement.Api.Application.Abstractions;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
    Task AddAsync(Customer customer, CancellationToken ct = default);
}

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);
    Task<bool> ExistsBySkuAsync(string sku, CancellationToken ct = default);
    Task AddAsync(Product product, CancellationToken ct = default);
}

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> GetSubmittedBeforeAsync(DateTime submittedBeforeUtc, CancellationToken ct = default);
    Task AddAsync(Order order, CancellationToken ct = default);

    /// <summary>
    /// Explicitly tracks a line item newly added to an already-persisted order as inserted.
    /// Client-generated keys otherwise cause EF's graph heuristic to treat it as an update.
    /// </summary>
    void TrackAddedLineItem(LineItem lineItem);
}

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct = default);
}
