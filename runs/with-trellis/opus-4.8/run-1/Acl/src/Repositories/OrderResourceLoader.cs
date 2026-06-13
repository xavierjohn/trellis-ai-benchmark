namespace OrderManagement.AntiCorruptionLayer.Repositories;

using OrderManagement.Application.Abstractions;
using OrderManagement.Domain;
using Trellis.Authorization;
using Trellis.EntityFrameworkCore;

/// <summary>Shared resource loader used by the cancel-order ownership check.</summary>
public sealed class OrderResourceLoader(IOrderRepository repository) : SharedResourceLoaderById<Order, OrderId>
{
    /// <inheritdoc />
    public override async Task<Result<Order>> GetByIdAsync(OrderId id, CancellationToken cancellationToken)
    {
        var maybe = await repository.FindByIdAsync(id, cancellationToken);
        return maybe.ToResult(new Error.NotFound(ResourceRef.For<Order>(id.Value))
        {
            Detail = "Order not found.",
        });
    }
}
