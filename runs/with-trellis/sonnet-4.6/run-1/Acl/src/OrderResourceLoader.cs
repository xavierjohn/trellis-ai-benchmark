namespace OrderManagement.AntiCorruptionLayer;

using OrderManagement.Application;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>
/// Shared order resource loader for resource authorization.
/// </summary>
internal sealed class OrderResourceLoader(IOrderRepository repository) : SharedResourceLoaderById<Order, OrderId>
{
    /// <inheritdoc />
    public override async Task<Result<Order>> GetByIdAsync(OrderId id, CancellationToken cancellationToken)
    {
        var maybeOrder = await repository.FindByIdAsync(id, cancellationToken);
        if (!maybeOrder.TryGetValue(out var order))
        {
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Order>(id))
            {
                Detail = $"Order {id} not found.",
            });
        }

        return Result.Ok(order);
    }
}
