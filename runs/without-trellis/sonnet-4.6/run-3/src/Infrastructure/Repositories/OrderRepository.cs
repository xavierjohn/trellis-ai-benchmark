using Application.Interfaces;
using Domain.Orders;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;

    public OrderRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.Orders.Include(o => o.LineItems).FirstOrDefaultAsync(o => o.OrderId == id, ct);

    public async Task AddAsync(Order order, CancellationToken ct = default)
    {
        await _context.Orders.AddAsync(order, ct);
    }

    public Task<List<Order>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default) =>
        _context.Orders.Include(o => o.LineItems).Where(o => o.CustomerId == customerId).ToListAsync(ct);

    public Task<List<Order>> GetOverdueAsync(TimeProvider timeProvider, CancellationToken ct = default)
    {
        var cutoff = timeProvider.GetUtcNow().UtcDateTime.AddDays(-7);
        return _context.Orders
            .Include(o => o.LineItems)
            .Where(o => o.Status == OrderStatus.Submitted && o.SubmittedAt != null && o.SubmittedAt < cutoff)
            .ToListAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _context.SaveChangesAsync(ct);
}
