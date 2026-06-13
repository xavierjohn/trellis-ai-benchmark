namespace OrderManagement.Application;

using Mediator;
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
public sealed class RemoveLineItemCommandHandler(IOrderRepository orders)
    : ICommandHandler<RemoveLineItemCommand, Result<Order>>
{
    /// <inheritdoc />
    public async ValueTask<Result<Order>> Handle(RemoveLineItemCommand command, CancellationToken cancellationToken)
    {
        var maybeOrder = await orders.FindByIdAsync(command.OrderId, cancellationToken);
        return maybeOrder
            .ToResult(new Error.NotFound(ResourceRef.For<Order>(command.OrderId)) { Detail = "Order not found." })
            .Bind(order => order.RemoveLineItem(command.LineItemId));
    }
}
