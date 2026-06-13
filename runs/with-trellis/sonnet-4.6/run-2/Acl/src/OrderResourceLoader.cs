namespace OrderManagement.AntiCorruptionLayer;

using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain;
using Trellis.Authorization;
using Trellis.EntityFrameworkCore;

/// <summary>Shared resource loader for Order authorization.</summary>
internal sealed class OrderResourceLoader : SharedResourceLoaderById<Order, OrderId>
{
    private readonly AppDbContext _context;

    public OrderResourceLoader(AppDbContext context) => _context = context;

    public override async Task<Result<Order>> GetByIdAsync(OrderId id, CancellationToken cancellationToken) =>
        await _context.Orders
            .Include(o => o.LineItems)
            .Where(o => o.Id == id)
            .FirstOrDefaultResultAsync(
                new Error.NotFound(ResourceRef.For<Order>(id)) { Detail = $"Order {id.Value} not found." },
                cancellationToken);
}
