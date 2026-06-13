namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>
/// Cancels an order.
/// </summary>
public sealed record CancelOrderCommand(OrderId OrderId)
    : ICommand<Result<Order>>, IAuthorize, IAuthorizeResource<Order>, IIdentifyResource<Order, OrderId>
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersCancel];

    /// <inheritdoc />
    public IResult Authorize(Actor actor, Order resource) =>
        Result.Ensure(actor.HasPermission(Permissions.OrdersReadAll) || actor.IsOwner(resource.CreatedByActorId),
            new Error.Forbidden("order.cancel.not_owner", ResourceRef.For<Order>(OrderId))
            {
                Detail = "Only the order owner or an actor with orders:read-all can cancel this order.",
            });

    /// <inheritdoc />
    public OrderId GetResourceId() => OrderId;
}

/// <summary>
/// Handles <see cref="CancelOrderCommand"/>.
/// </summary>
public sealed class CancelOrderCommandHandler(IOrderRepository orderRepository, IProductRepository productRepository, TimeProvider timeProvider)
    : ICommandHandler<CancelOrderCommand, Result<Order>>
{
    /// <inheritdoc />
    public async ValueTask<Result<Order>> Handle(CancelOrderCommand command, CancellationToken cancellationToken)
    {
        var maybeOrder = await orderRepository.FindByIdAsync(command.OrderId, cancellationToken);
        if (!maybeOrder.TryGetValue(out var order))
        {
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Order>(command.OrderId))
            {
                Detail = $"Order {command.OrderId} not found.",
            });
        }

        var products = await productRepository.GetByIdsAsync(order.LineItems.Select(lineItem => lineItem.ProductId).Distinct(), cancellationToken);
        return order.Cancel(products, timeProvider).Map(_ => order);
    }
}
