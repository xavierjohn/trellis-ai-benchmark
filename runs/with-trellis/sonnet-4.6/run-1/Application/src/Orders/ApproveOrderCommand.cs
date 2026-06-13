namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>
/// Approves a submitted order.
/// </summary>
public sealed record ApproveOrderCommand(OrderId OrderId) : ICommand<Result<Order>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersApprove];
}

/// <summary>
/// Handles <see cref="ApproveOrderCommand"/>.
/// </summary>
public sealed class ApproveOrderCommandHandler(IOrderRepository orderRepository, TimeProvider timeProvider)
    : ICommandHandler<ApproveOrderCommand, Result<Order>>
{
    /// <inheritdoc />
    public async ValueTask<Result<Order>> Handle(ApproveOrderCommand command, CancellationToken cancellationToken)
    {
        var maybeOrder = await orderRepository.FindByIdAsync(command.OrderId, cancellationToken);
        if (!maybeOrder.TryGetValue(out var order))
        {
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Order>(command.OrderId))
            {
                Detail = $"Order {command.OrderId} not found.",
            });
        }

        return order.Approve(timeProvider).Map(_ => order);
    }
}
