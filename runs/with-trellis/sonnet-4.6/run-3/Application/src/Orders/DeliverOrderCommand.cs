namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Domain;
using Trellis;
using Trellis.Authorization;

/// <summary>Marks a shipped order as delivered.</summary>
public sealed record DeliverOrderCommand(OrderId OrderId) : ICommand<Result<Order>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersDeliver];
}

/// <summary>Handler for <see cref="DeliverOrderCommand"/>.</summary>
internal sealed class DeliverOrderCommandHandler(
    IOrderRepository orderRepository,
    TimeProvider timeProvider) : ICommandHandler<DeliverOrderCommand, Result<Order>>
{
    public async ValueTask<Result<Order>> Handle(DeliverOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await orderRepository.FindByIdAsync(command.OrderId, cancellationToken)
            .ToResultAsync(new Error.NotFound(ResourceRef.For<Order>(command.OrderId)) { Detail = $"Order {command.OrderId.Value} not found." });

        return order.Bind(foundOrder => foundOrder.Deliver(timeProvider).Map(_ => foundOrder));
    }
}
