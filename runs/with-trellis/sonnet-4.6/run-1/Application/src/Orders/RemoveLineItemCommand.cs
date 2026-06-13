namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>
/// Removes a line item from a draft order.
/// </summary>
public sealed record RemoveLineItemCommand(OrderId OrderId, LineItemId LineItemId)
    : ICommand<Result<Order>>, IAuthorize, IAuthorizeResource<Order>, IIdentifyResource<Order, OrderId>
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersCreate];

    /// <inheritdoc />
    public IResult Authorize(Actor actor, Order resource) =>
        Result.Ensure(actor.IsOwner(resource.CreatedByActorId), new Error.Forbidden("order.modify.owner", ResourceRef.For<Order>(OrderId))
        {
            Detail = "Only the order creator can modify draft line items.",
        });

    /// <inheritdoc />
    public OrderId GetResourceId() => OrderId;
}

/// <summary>
/// Handles <see cref="RemoveLineItemCommand"/>.
/// </summary>
public sealed class RemoveLineItemCommandHandler(IOrderRepository orderRepository)
    : ICommandHandler<RemoveLineItemCommand, Result<Order>>
{
    /// <inheritdoc />
    public async ValueTask<Result<Order>> Handle(RemoveLineItemCommand command, CancellationToken cancellationToken)
    {
        var maybeOrder = await orderRepository.FindByIdAsync(command.OrderId, cancellationToken);
        if (!maybeOrder.TryGetValue(out var order))
        {
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Order>(command.OrderId))
            {
                Detail = $"Order {command.OrderId} not found.",
            });
        }

        return order.RemoveLineItem(command.LineItemId).Map(_ => order);
    }
}
