namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Application.Abstractions;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>
/// Cancels an order. Requires the <c>orders:cancel</c> permission plus an ownership check:
/// the actor must be the order creator, or hold <c>orders:read-all</c> (admin).
/// </summary>
public sealed record CancelOrderCommand(OrderId OrderId)
    : ICommand<Result<Order>>, IAuthorize, IAuthorizeResource<Order>, IIdentifyResource<Order, OrderId>
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions => [Permissions.OrdersCancel];

    /// <inheritdoc />
    public OrderId GetResourceId() => OrderId;

    /// <inheritdoc />
    public IResult Authorize(Actor actor, Order resource) =>
        actor.IsOwner(resource.CreatedByActorId) || actor.HasPermission(Permissions.OrdersReadAll)
            ? Result.Ok()
            : Result.Fail(new Error.Forbidden("orders.cancel.owner", ResourceRef.For<Order>(OrderId))
            {
                Detail = "Only the order creator or an administrator can cancel this order.",
            });
}

/// <summary>Handles <see cref="CancelOrderCommand"/>.</summary>
public sealed class CancelOrderCommandHandler(
    IOrderRepository orderRepository,
    IProductRepository productRepository,
    TimeProvider timeProvider)
    : ICommandHandler<CancelOrderCommand, Result<Order>>
{
    /// <inheritdoc />
    public async ValueTask<Result<Order>> Handle(CancelOrderCommand command, CancellationToken cancellationToken)
    {
        var maybe = await orderRepository.FindByIdAsync(command.OrderId, cancellationToken);
        if (!maybe.TryGetValue(out var order))
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Order>(command.OrderId.Value))
            {
                Detail = "Order not found.",
            });

        var shouldReleaseStock = order.ReservesStock;

        var cancelResult = order.Cancel(timeProvider);
        if (cancelResult.IsFailure)
            return cancelResult;

        if (shouldReleaseStock)
        {
            var ids = order.LineItems.Select(li => li.ProductId).Distinct().ToArray();
            var products = await productRepository.GetByIdsAsync(ids, cancellationToken);
            var byId = products.ToDictionary(p => p.Id);

            foreach (var group in order.LineItems.GroupBy(li => li.ProductId))
            {
                if (byId.TryGetValue(group.Key, out var product))
                    product.ReleaseStock(group.Sum(li => li.Quantity.Value));
            }
        }

        return cancelResult;
    }
}
