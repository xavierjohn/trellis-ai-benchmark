namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Application.Abstractions;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>Marks a shipped order as delivered.</summary>
public sealed record DeliverOrderCommand(OrderId OrderId) : ICommand<Result<Order>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions => [Permissions.OrdersDeliver];
}

/// <summary>Handles <see cref="DeliverOrderCommand"/>.</summary>
public sealed class DeliverOrderCommandHandler(IOrderRepository orderRepository, TimeProvider timeProvider)
    : ICommandHandler<DeliverOrderCommand, Result<Order>>
{
    /// <inheritdoc />
    public async ValueTask<Result<Order>> Handle(DeliverOrderCommand command, CancellationToken cancellationToken)
    {
        var maybe = await orderRepository.FindByIdAsync(command.OrderId, cancellationToken);
        return maybe
            .ToResult(new Error.NotFound(ResourceRef.For<Order>(command.OrderId.Value)) { Detail = "Order not found." })
            .Bind(order => order.Deliver(timeProvider));
    }
}
