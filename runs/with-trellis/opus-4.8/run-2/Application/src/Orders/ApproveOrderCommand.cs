namespace OrderManagement.Application;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>Approves a submitted order (Submitted → Approved).</summary>
public sealed record ApproveOrderCommand(OrderId OrderId) : ICommand<Result<Order>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions => [Permissions.OrdersApprove];
}

/// <summary>Handles <see cref="ApproveOrderCommand"/>.</summary>
public sealed class ApproveOrderCommandHandler(IOrderRepository orders, TimeProvider timeProvider)
    : ICommandHandler<ApproveOrderCommand, Result<Order>>
{
    /// <inheritdoc />
    public async ValueTask<Result<Order>> Handle(ApproveOrderCommand command, CancellationToken cancellationToken)
    {
        var maybeOrder = await orders.FindByIdAsync(command.OrderId, cancellationToken);
        return maybeOrder
            .ToResult(new Error.NotFound(ResourceRef.For<Order>(command.OrderId)) { Detail = "Order not found." })
            .Bind(order => order.Approve(timeProvider));
    }
}
