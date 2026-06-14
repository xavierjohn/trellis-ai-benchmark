using Domain.Orders;

namespace Application.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Order order, CancellationToken ct = default);
    Task<List<Order>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
    Task<List<Order>> GetOverdueAsync(TimeProvider timeProvider, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
