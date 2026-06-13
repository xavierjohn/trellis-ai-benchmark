namespace OrderManagement.Application.Abstractions;

using OrderManagement.Domain;

/// <summary>Persistence operations for the <see cref="Order"/> aggregate.</summary>
public interface IOrderRepository
{
    /// <summary>Finds an order (with its line items) by ID.</summary>
    Task<Maybe<Order>> FindByIdAsync(OrderId id, CancellationToken cancellationToken);

    /// <summary>Lists all orders belonging to a customer.</summary>
    Task<IReadOnlyList<Order>> ListByCustomerAsync(CustomerId customerId, CancellationToken cancellationToken);

    /// <summary>Lists overdue orders as of the supplied timestamp.</summary>
    Task<IReadOnlyList<Order>> ListOverdueAsync(DateTime asOf, CancellationToken cancellationToken);

    /// <summary>Stages a new order for insertion.</summary>
    void Add(Order order);
}
