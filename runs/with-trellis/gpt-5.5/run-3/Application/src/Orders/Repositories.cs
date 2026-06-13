namespace OrderManagement.Application.Orders;

using OrderManagement.Domain;
using Trellis.Primitives;

public interface ICustomerRepository
{
    Task<Maybe<Customer>> FindByIdAsync(CustomerId id, CancellationToken cancellationToken);

    Task<bool> ExistsByEmailAsync(EmailAddress email, CancellationToken cancellationToken);

    void Add(Customer customer);
}

public interface IProductRepository
{
    Task<Maybe<Product>> FindByIdAsync(ProductId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Product>> FindByIdsAsync(IReadOnlyCollection<ProductId> ids, CancellationToken cancellationToken);

    Task<bool> ExistsBySkuAsync(Sku sku, CancellationToken cancellationToken);

    void Add(Product product);
}

public interface IOrderRepository
{
    Task<Maybe<Order>> FindByIdAsync(OrderId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Order>> ListByCustomerAsync(CustomerId customerId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Order>> ListOverdueAsync(DateTime asOfUtc, CancellationToken cancellationToken);

    void Add(Order order);
}
