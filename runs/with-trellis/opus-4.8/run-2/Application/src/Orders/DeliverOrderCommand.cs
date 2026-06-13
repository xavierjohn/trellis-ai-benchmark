namespace OrderManagement.Application;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>Marks a shipped order delivered (Shipped → Delivered).</summary>
public sealed record DeliverOrderCommand(OrderId OrderId) : ICommand<Result<Order>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions => [Permissions.OrdersDeliver];
}

/// <summary>Handles <see cref="DeliverOrderCommand"/>.</summary>
public sealed class DeliverOrderCommandHandler(IOrderRepository orders, TimeProvider timeProvider)
    : ICommandHandler<DeliverOrderCommand, Result<Order>>
{
    /// <inheritdoc />
    public async ValueTask<Result<Order>> Handle(DeliverOrderCommand command, CancellationToken cancellationToken)
    {
        var maybeOrder = await orders.FindByIdAsync(command.OrderId, cancellationToken);
        return maybeOrder
            .ToResult(new Error.NotFound(ResourceRef.For<Order>(command.OrderId)) { Detail = "Order not found." })
            .Bind(order => order.Deliver(timeProvider));
    }
}
