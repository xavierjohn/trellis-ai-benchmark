namespace OrderManagement.Application;

using OrderManagement.Domain;

/// <summary>Customer repository.</summary>
public interface ICustomerRepository
{
    Task<Maybe<Customer>> FindByIdAsync(CustomerId id, CancellationToken cancellationToken);
    Task<Maybe<Customer>> FindByEmailAsync(Trellis.Primitives.EmailAddress email, CancellationToken cancellationToken);
    void Add(Customer customer);
}

/// <summary>Product repository.</summary>
public interface IProductRepository
{
    Task<Maybe<Product>> FindByIdAsync(ProductId id, CancellationToken cancellationToken);
    Task<Maybe<Product>> FindBySkuAsync(Sku sku, CancellationToken cancellationToken);
    Task<IReadOnlyList<Product>> FindByIdsAsync(IReadOnlyCollection<ProductId> ids, CancellationToken cancellationToken);
    void Add(Product product);
}

/// <summary>Order repository.</summary>
public interface IOrderRepository
{
    Task<Maybe<Order>> FindByIdAsync(OrderId id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Order>> ListByCustomerAsync(CustomerId customerId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Order>> ListOverdueAsync(DateTimeOffset asOf, CancellationToken cancellationToken);
    void Add(Order order);
}
