namespace OrderManagement.Application;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>Ships an approved order (Approved → Shipped).</summary>
public sealed record ShipOrderCommand(OrderId OrderId) : ICommand<Result<Order>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions => [Permissions.OrdersShip];
}

/// <summary>Handles <see cref="ShipOrderCommand"/>.</summary>
public sealed class ShipOrderCommandHandler(IOrderRepository orders, TimeProvider timeProvider)
    : ICommandHandler<ShipOrderCommand, Result<Order>>
{
    /// <inheritdoc />
    public async ValueTask<Result<Order>> Handle(ShipOrderCommand command, CancellationToken cancellationToken)
    {
        var maybeOrder = await orders.FindByIdAsync(command.OrderId, cancellationToken);
        return maybeOrder
            .ToResult(new Error.NotFound(ResourceRef.For<Order>(command.OrderId)) { Detail = "Order not found." })
            .Bind(order => order.Ship(timeProvider));
    }
}
