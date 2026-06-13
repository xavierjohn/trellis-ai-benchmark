using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions;
using OrderManagement.Domain.Orders;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Infrastructure.Repositories;

public sealed class OrderRepository(OrderManagementDbContext db) : IOrderRepository
{
    public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.Orders.Include(o => o.LineItems).FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<IReadOnlyList<Order>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default)
        => await db.Orders.Include(o => o.LineItems)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Order>> GetSubmittedBeforeAsync(DateTimeOffset cutoff, CancellationToken ct = default)
    {
        // SQLite cannot translate DateTimeOffset comparisons, so filter by the (translatable)
        // status in the database and apply the time threshold in memory. The
        // Status+SubmittedAt index still serves the status predicate efficiently.
        var submitted = await db.Orders.Include(o => o.LineItems)
            .Where(o => o.Status == OrderStatus.Submitted)
            .ToListAsync(ct);

        return submitted.Where(o => o.SubmittedAt is { } s && s < cutoff).ToList();
    }

    public async Task AddAsync(Order order, CancellationToken ct = default)
        => await db.Orders.AddAsync(order, ct);
}
