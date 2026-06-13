namespace OrderManagement.Application;

using OrderManagement.Domain;

/// <summary>Persistence contract for the <see cref="Order"/> aggregate.</summary>
public interface IOrderRepository
{
    /// <summary>Finds an order (with its line items) by id, or <see cref="Maybe{T}.None"/> when absent.</summary>
    Task<Maybe<Order>> FindByIdAsync(OrderId id, CancellationToken cancellationToken);

    /// <summary>Queries orders matching the supplied specification.</summary>
    Task<IReadOnlyList<Order>> QueryAsync(Specification<Order> specification, CancellationToken cancellationToken);

    /// <summary>Stages a new order for insertion. The unit of work commits on success.</summary>
    void Add(Order order);
}
