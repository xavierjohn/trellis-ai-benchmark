namespace OrderManagement.AntiCorruptionLayer;

using OrderManagement.Application;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>
/// Shared resource loader used by resource-authorized order commands (e.g. cancel).
/// </summary>
public sealed class OrderResourceLoader(IOrderRepository orders) : SharedResourceLoaderById<Order, OrderId>
{
    /// <inheritdoc />
    public override async Task<Result<Order>> GetByIdAsync(OrderId id, CancellationToken cancellationToken)
    {
        var maybe = await orders.FindByIdAsync(id, cancellationToken);
        return maybe.ToResult(new Error.NotFound(ResourceRef.For<Order>(id)) { Detail = "Order not found." });
    }
}
