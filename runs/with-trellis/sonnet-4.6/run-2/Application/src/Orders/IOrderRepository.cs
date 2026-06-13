namespace OrderManagement.Application.Orders;

using OrderManagement.Domain;

/// <summary>Repository for order aggregates.</summary>
public interface IOrderRepository
{
    /// <summary>Finds an order by its unique identifier.</summary>
    Task<Maybe<Order>> FindByIdAsync(OrderId id, CancellationToken cancellationToken);

    /// <summary>Queries orders matching the given specification.</summary>
    Task<IReadOnlyList<Order>> QueryAsync(Specification<Order> specification, CancellationToken cancellationToken);

    /// <summary>Adds a new order to the repository.</summary>
    void Add(Order order);
}
