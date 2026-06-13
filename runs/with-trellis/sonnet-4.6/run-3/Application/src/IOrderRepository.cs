namespace OrderManagement.Application;

using OrderManagement.Domain;
using Trellis;

/// <summary>Repository interface for Order persistence.</summary>
public interface IOrderRepository
{
    Task<Maybe<Order>> FindByIdAsync(OrderId id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Order>> FindByCustomerIdAsync(CustomerId customerId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Order>> QueryAsync(Specification<Order> specification, CancellationToken cancellationToken);
    void Add(Order order);
}
