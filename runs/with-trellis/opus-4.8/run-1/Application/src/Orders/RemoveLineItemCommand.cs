namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Application.Abstractions;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>Removes a line item from a draft order.</summary>
public sealed record RemoveLineItemCommand(OrderId OrderId, LineItemId LineItemId)
    : ICommand<Result<Order>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions => [Permissions.OrdersCreate];
}

/// <summary>Handles <see cref="RemoveLineItemCommand"/>.</summary>
public sealed class RemoveLineItemCommandHandler(IOrderRepository orderRepository)
    : ICommandHandler<RemoveLineItemCommand, Result<Order>>
{
    /// <inheritdoc />
    public async ValueTask<Result<Order>> Handle(RemoveLineItemCommand command, CancellationToken cancellationToken)
    {
        var orderMaybe = await orderRepository.FindByIdAsync(command.OrderId, cancellationToken);
        if (!orderMaybe.TryGetValue(out var order))
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Order>(command.OrderId.Value))
            {
                Detail = "Order not found.",
            });

        return order.RemoveLineItem(command.LineItemId);
    }
}
