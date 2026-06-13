namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Domain;
using Trellis;
using Trellis.Authorization;

/// <summary>Approves a submitted order.</summary>
public sealed record ApproveOrderCommand(OrderId OrderId) : ICommand<Result<Order>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersApprove];
}

/// <summary>Handler for <see cref="ApproveOrderCommand"/>.</summary>
internal sealed class ApproveOrderCommandHandler(
    IOrderRepository orderRepository,
    TimeProvider timeProvider) : ICommandHandler<ApproveOrderCommand, Result<Order>>
{
    public async ValueTask<Result<Order>> Handle(ApproveOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await orderRepository.FindByIdAsync(command.OrderId, cancellationToken)
            .ToResultAsync(new Error.NotFound(ResourceRef.For<Order>(command.OrderId)) { Detail = $"Order {command.OrderId.Value} not found." });

        return order.Bind(foundOrder => foundOrder.Approve(timeProvider).Map(_ => foundOrder));
    }
}
