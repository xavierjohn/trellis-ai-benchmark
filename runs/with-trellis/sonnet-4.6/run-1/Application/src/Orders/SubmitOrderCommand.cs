namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>
/// Submits a draft order.
/// </summary>
public sealed record SubmitOrderCommand(OrderId OrderId)
    : ICommand<Result<Order>>, IAuthorize, IAuthorizeResource<Order>, IIdentifyResource<Order, OrderId>
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersSubmit];

    /// <inheritdoc />
    public IResult Authorize(Actor actor, Order resource) =>
        Result.Ensure(actor.IsOwner(resource.CreatedByActorId), new Error.Forbidden("order.submit.owner", ResourceRef.For<Order>(OrderId))
        {
            Detail = "Only the order creator can submit the order.",
        });

    /// <inheritdoc />
    public OrderId GetResourceId() => OrderId;
}

/// <summary>
/// Handles <see cref="SubmitOrderCommand"/>.
/// </summary>
public sealed class SubmitOrderCommandHandler(IOrderRepository orderRepository, IProductRepository productRepository, TimeProvider timeProvider)
    : ICommandHandler<SubmitOrderCommand, Result<Order>>
{
    /// <inheritdoc />
    public async ValueTask<Result<Order>> Handle(SubmitOrderCommand command, CancellationToken cancellationToken)
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
        return order.Submit(products, timeProvider).Map(_ => order);
    }
}
