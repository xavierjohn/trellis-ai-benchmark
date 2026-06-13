namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Domain;
using Trellis;
using Trellis.Authorization;

/// <summary>
/// Cancels an order. Requires ownership: actor must be the order creator,
/// or have <c>orders:read-all</c> permission.
/// Releases reserved stock if the order was submitted or approved.
/// </summary>
public sealed class CancelOrderCommand : ICommand<Result<Order>>, IAuthorize, IAuthorizeResource<Order>, IIdentifyResource<Order, OrderId>
{
    public CancelOrderCommand(OrderId orderId) => OrderId = orderId;

    public OrderId OrderId { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersCancel];

    /// <inheritdoc />
    public IResult Authorize(Actor actor, Order resource) =>
        actor.IsOwner(resource.CreatedByActorId) || actor.HasPermission(Permissions.OrdersReadAll)
            ? Result.Ok()
            : Result.Fail(new Error.Forbidden("order.cancel.ownership", ResourceRef.For<Order>(OrderId)) { Detail = "You can only cancel orders you created." });

    /// <inheritdoc />
    public OrderId GetResourceId() => OrderId;
}

/// <summary>Handler for <see cref="CancelOrderCommand"/>.</summary>
internal sealed class CancelOrderCommandHandler(
    IOrderRepository orderRepository,
    IProductRepository productRepository,
    TimeProvider timeProvider) : ICommandHandler<CancelOrderCommand, Result<Order>>
{
    public async ValueTask<Result<Order>> Handle(CancelOrderCommand command, CancellationToken cancellationToken)
    {
        var orderResult = await orderRepository.FindByIdAsync(command.OrderId, cancellationToken)
            .ToResultAsync(new Error.NotFound(ResourceRef.For<Order>(command.OrderId)) { Detail = $"Order {command.OrderId.Value} not found." });
        if (!orderResult.TryGetValue(out var order))
            return Result.Fail<Order>(orderResult.Error!);

        var previousStatus = order.Status;
        Dictionary<ProductId, Product>? productsById = null;
        if (previousStatus == OrderStatus.Submitted || previousStatus == OrderStatus.Approved)
        {
            var productIds = order.LineItems.Select(lineItem => lineItem.ProductId).ToList();
            var products = await productRepository.FindByIdsAsync(productIds, cancellationToken);
            productsById = products.ToDictionary(product => product.Id);

            foreach (var productId in productIds)
            {
                if (!productsById.ContainsKey(productId))
                {
                    return Result.Fail<Order>(
                        new Error.NotFound(ResourceRef.For<Product>(productId))
                        {
                            Detail = $"Product {productId.Value} not found.",
                        });
                }
            }
        }

        var cancelResult = order.Cancel(timeProvider);
        if (!cancelResult.TryGetValue(out _))
            return Result.Fail<Order>(cancelResult.Error!);

        if (productsById is not null)
        {
            foreach (var lineItem in order.LineItems)
                productsById[lineItem.ProductId].ReleaseStock(lineItem.Quantity);
        }

        return Result.Ok(order);
    }
}
