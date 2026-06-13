namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Domain;
using Trellis;
using Trellis.Authorization;

/// <summary>Ships an approved order.</summary>
public sealed record ShipOrderCommand(OrderId OrderId) : ICommand<Result<Order>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersShip];
}

/// <summary>Handler for <see cref="ShipOrderCommand"/>.</summary>
internal sealed class ShipOrderCommandHandler(
    IOrderRepository orderRepository,
    TimeProvider timeProvider) : ICommandHandler<ShipOrderCommand, Result<Order>>
{
    public async ValueTask<Result<Order>> Handle(ShipOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await orderRepository.FindByIdAsync(command.OrderId, cancellationToken)
            .ToResultAsync(new Error.NotFound(ResourceRef.For<Order>(command.OrderId)) { Detail = $"Order {command.OrderId.Value} not found." });

        return order.Bind(foundOrder => foundOrder.Ship(timeProvider).Map(_ => foundOrder));
    }
}
