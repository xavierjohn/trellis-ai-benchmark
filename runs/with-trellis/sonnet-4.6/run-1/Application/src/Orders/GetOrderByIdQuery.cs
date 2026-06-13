namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>
/// Loads a single order by identifier.
/// </summary>
public sealed record GetOrderByIdQuery(OrderId OrderId)
    : IQuery<Result<Order>>, IAuthorizeResource<Order>, IIdentifyResource<Order, OrderId>
{
    /// <inheritdoc />
    public IResult Authorize(Actor actor, Order resource) =>
        Result.Ensure(actor.HasPermission(Permissions.OrdersReadAll) || (actor.HasPermission(Permissions.OrdersRead) && actor.IsOwner(resource.CreatedByActorId)),
            new Error.Forbidden("order.read.forbidden", ResourceRef.For<Order>(OrderId))
            {
                Detail = "Only the order owner or an actor with orders:read-all can read this order.",
            });

    /// <inheritdoc />
    public OrderId GetResourceId() => OrderId;
}

/// <summary>
/// Handles <see cref="GetOrderByIdQuery"/>.
/// </summary>
public sealed class GetOrderByIdQueryHandler(IOrderRepository orderRepository) : IQueryHandler<GetOrderByIdQuery, Result<Order>>
{
    /// <inheritdoc />
    public async ValueTask<Result<Order>> Handle(GetOrderByIdQuery query, CancellationToken cancellationToken)
    {
        var maybeOrder = await orderRepository.FindByIdAsync(query.OrderId, cancellationToken);
        if (!maybeOrder.TryGetValue(out var order))
        {
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Order>(query.OrderId))
            {
                Detail = $"Order {query.OrderId} not found.",
            });
        }

        return Result.Ok(order);
    }
}
