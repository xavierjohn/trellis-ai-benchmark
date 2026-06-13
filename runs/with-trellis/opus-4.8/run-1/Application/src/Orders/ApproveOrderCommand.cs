namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Application.Abstractions;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>Approves a submitted order.</summary>
public sealed record ApproveOrderCommand(OrderId OrderId) : ICommand<Result<Order>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions => [Permissions.OrdersApprove];
}

/// <summary>Handles <see cref="ApproveOrderCommand"/>.</summary>
public sealed class ApproveOrderCommandHandler(IOrderRepository orderRepository, TimeProvider timeProvider)
    : ICommandHandler<ApproveOrderCommand, Result<Order>>
{
    /// <inheritdoc />
    public async ValueTask<Result<Order>> Handle(ApproveOrderCommand command, CancellationToken cancellationToken)
    {
        var maybe = await orderRepository.FindByIdAsync(command.OrderId, cancellationToken);
        return maybe
            .ToResult(new Error.NotFound(ResourceRef.For<Order>(command.OrderId.Value)) { Detail = "Order not found." })
            .Bind(order => order.Approve(timeProvider));
    }
}
