namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Application.Abstractions;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>Ships an approved order.</summary>
public sealed record ShipOrderCommand(OrderId OrderId) : ICommand<Result<Order>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions => [Permissions.OrdersShip];
}

/// <summary>Handles <see cref="ShipOrderCommand"/>.</summary>
public sealed class ShipOrderCommandHandler(IOrderRepository orderRepository, TimeProvider timeProvider)
    : ICommandHandler<ShipOrderCommand, Result<Order>>
{
    /// <inheritdoc />
    public async ValueTask<Result<Order>> Handle(ShipOrderCommand command, CancellationToken cancellationToken)
    {
        var maybe = await orderRepository.FindByIdAsync(command.OrderId, cancellationToken);
        return maybe
            .ToResult(new Error.NotFound(ResourceRef.For<Order>(command.OrderId.Value)) { Detail = "Order not found." })
            .Bind(order => order.Ship(timeProvider));
    }
}
