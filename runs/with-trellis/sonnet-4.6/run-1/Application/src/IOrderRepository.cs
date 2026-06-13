namespace OrderManagement.Application;

using OrderManagement.Domain;

/// <summary>
/// Order repository contract.
/// </summary>
public interface IOrderRepository
{
    /// <summary>Finds an order by identifier.</summary>
    Task<Maybe<Order>> FindByIdAsync(OrderId id, CancellationToken cancellationToken);

    /// <summary>Loads orders for a customer.</summary>
    Task<IReadOnlyList<Order>> GetByCustomerIdAsync(CustomerId customerId, CancellationToken cancellationToken);

    /// <summary>Queries orders matching a specification.</summary>
    Task<IReadOnlyList<Order>> QueryAsync(Specification<Order> specification, CancellationToken cancellationToken);

    /// <summary>Stages an order for insertion.</summary>
    void Add(Order order);
}
