namespace OrderManagement.AntiCorruptionLayer;

using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Orders;
using OrderManagement.Domain;
using Trellis.EntityFrameworkCore;

/// <summary>EF Core implementation of IOrderRepository.</summary>
internal class OrderRepository : RepositoryBase<Order, OrderId>, IOrderRepository
{
    private readonly AppDbContext _context;

    public OrderRepository(AppDbContext context) : base(context)
    {
        _context = context;
    }

    public override async Task<Maybe<Order>> FindByIdAsync(OrderId id, CancellationToken cancellationToken) =>
        await _context.Orders
            .Include(o => o.LineItems)
            .Where(o => o.Id == id)
            .FirstOrDefaultMaybeAsync(cancellationToken);

    public override async Task<IReadOnlyList<Order>> QueryAsync(Specification<Order> specification, CancellationToken cancellationToken) =>
        await _context.Orders
            .Include(o => o.LineItems)
            .Where(specification)
            .ToListAsync(cancellationToken);
}
