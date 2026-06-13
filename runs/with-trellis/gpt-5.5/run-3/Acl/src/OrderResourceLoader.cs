namespace OrderManagement.AntiCorruptionLayer;

using OrderManagement.Application.Orders;
using OrderManagement.Domain;
using Trellis.Authorization;

internal sealed class OrderResourceLoader : SharedResourceLoaderById<Order, OrderId>
{
    private readonly IOrderRepository _orders;

    public OrderResourceLoader(IOrderRepository orders) => _orders = orders;

    public override async Task<Result<Order>> GetByIdAsync(OrderId id, CancellationToken cancellationToken) =>
        (await _orders.FindByIdAsync(id, cancellationToken))
            .ToResult(new Error.NotFound(ResourceRef.For<Order>(id)) { Detail = "Order not found." });
}
