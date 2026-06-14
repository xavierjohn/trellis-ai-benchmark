namespace OrderManagement.AntiCorruptionLayer;

using OrderManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Trellis.Authorization;
using Trellis.EntityFrameworkCore;

/// <summary>Shared order loader for resource authorization.</summary>
internal sealed class OrderResourceLoader(AppDbContext context) : SharedResourceLoaderById<Order, OrderId>
{
    /// <inheritdoc />
    public override async Task<Result<Order>> GetByIdAsync(OrderId id, CancellationToken cancellationToken) =>
        await context.Orders
            .Include(order => order.LineItems)
            .Where(order => order.Id == id)
            .FirstOrDefaultResultAsync(
                new Error.NotFound(ResourceRef.For<Order>(id)) { Detail = $"Order {id.Value} not found." },
                cancellationToken);
}
