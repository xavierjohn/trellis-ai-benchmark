namespace OrderManagement.Application.Orders;

using OrderManagement.Domain;

/// <summary>
/// Order persistence port.
/// </summary>
public interface IOrderRepository
{
    Task<Maybe<Order>> FindByIdAsync(OrderId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Order>> ListByCustomerAsync(CustomerId customerId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Order>> ListOverdueAsync(DateTime cutoff, CancellationToken cancellationToken);

    void Add(Order order);
}
