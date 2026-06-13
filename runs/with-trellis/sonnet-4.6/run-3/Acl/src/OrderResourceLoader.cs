namespace OrderManagement.AntiCorruptionLayer;

using OrderManagement.Application;
using OrderManagement.Domain;
using Trellis;
using Trellis.Authorization;

internal sealed class OrderResourceLoader(IOrderRepository repository) : SharedResourceLoaderById<Order, OrderId>
{
    public override async Task<Result<Order>> GetByIdAsync(OrderId id, CancellationToken cancellationToken)
    {
        var maybeOrder = await repository.FindByIdAsync(id, cancellationToken);
        return maybeOrder.ToResult(new Error.NotFound(ResourceRef.For<Order>(id)) { Detail = $"Order {id.Value} not found." });
    }
}
