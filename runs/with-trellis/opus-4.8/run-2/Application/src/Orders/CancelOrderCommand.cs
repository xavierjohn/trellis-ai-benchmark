namespace OrderManagement.Application;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>
/// Cancels an order. Requires the <c>orders:cancel</c> permission and an ownership check:
/// the actor must be the order's creator, or hold <c>orders:read-all</c> (admin).
/// </summary>
public sealed record CancelOrderCommand(OrderId OrderId)
    : ICommand<Result<Order>>, IAuthorize, IAuthorizeResource<Order>, IIdentifyResource<Order, OrderId>
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions => [Permissions.OrdersCancel];

    /// <inheritdoc />
    public OrderId GetResourceId() => OrderId;

    /// <inheritdoc />
    public Trellis.IResult Authorize(Actor actor, Order resource) =>
        actor.IsOwner(resource.CreatedByActorId) || actor.Permissions.Contains(Permissions.OrdersReadAll)
            ? Result.Ok()
            : Result.Fail(new Error.Forbidden(PolicyId: "orders.cancel.owner", Resource: ResourceRef.For<Order>(OrderId)));
}

/// <summary>Handles <see cref="CancelOrderCommand"/>.</summary>
public sealed class CancelOrderCommandHandler(IOrderRepository orders, IProductRepository products, TimeProvider timeProvider)
    : ICommandHandler<CancelOrderCommand, Result<Order>>
{
    /// <inheritdoc />
    public async ValueTask<Result<Order>> Handle(CancelOrderCommand command, CancellationToken cancellationToken)
    {
        var maybeOrder = await orders.FindByIdAsync(command.OrderId, cancellationToken);
        if (!maybeOrder.TryGetValue(out var order))
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Order>(command.OrderId)) { Detail = "Order not found." });

        var releasesStock = order.ReleasesStockOnCancel;
        Dictionary<ProductId, Product> byId = [];

        if (releasesStock)
        {
            var productIds = order.LineItems.Select(li => li.ProductId).Distinct().ToArray();
            var loaded = await products.GetByIdsAsync(productIds, cancellationToken);
            byId = loaded.ToDictionary(p => p.Id);

            var missing = productIds.Where(id => !byId.ContainsKey(id)).ToArray();
            if (missing.Length == 1)
                return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Product>(missing[0])) { Detail = "Product not found." });
            if (missing.Length > 1)
                return Result.Fail<Order>(new Error.Aggregate(missing.Select(id =>
                    (Error)new Error.NotFound(ResourceRef.For<Product>(id)) { Detail = "Product not found." }).ToArray()));
        }

        var cancelResult = order.Cancel(timeProvider);
        if (cancelResult.IsFailure)
            return cancelResult;

        if (releasesStock)
        {
            foreach (var group in order.LineItems.GroupBy(li => li.ProductId))
                byId[group.Key].ReleaseStock(group.Sum(li => li.Quantity.Value)).Discard();
        }

        return cancelResult;
    }
}
