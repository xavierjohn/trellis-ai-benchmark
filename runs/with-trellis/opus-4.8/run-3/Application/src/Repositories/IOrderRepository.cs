namespace OrderManagement.Application;

using OrderManagement.Domain;

/// <summary>Persistence contract for the <see cref="Order"/> aggregate.</summary>
public interface IOrderRepository
{
    /// <summary>Finds an order by ID, or <see cref="Maybe{T}.None"/> if absent.</summary>
    Task<Maybe<Order>> FindByIdAsync(OrderId id, CancellationToken cancellationToken);

    /// <summary>Returns all orders satisfying the specification.</summary>
    Task<IReadOnlyList<Order>> QueryAsync(Specification<Order> specification, CancellationToken cancellationToken);

    /// <summary>Returns all orders belonging to the given customer.</summary>
    Task<IReadOnlyList<Order>> ListByCustomerAsync(CustomerId customerId, CancellationToken cancellationToken);

    /// <summary>Stages an order for insertion. The unit-of-work commits on handler success.</summary>
    void Add(Order order);
}
