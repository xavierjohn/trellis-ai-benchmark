namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Domain;
using Trellis;
using Trellis.Authorization;

/// <summary>Removes a line item from a draft order.</summary>
public sealed record RemoveLineItemCommand(
    OrderId OrderId,
    LineItemId LineItemId) : ICommand<Result<Order>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersCreate];
}

/// <summary>Handler for <see cref="RemoveLineItemCommand"/>.</summary>
internal sealed class RemoveLineItemCommandHandler(
    IOrderRepository orderRepository) : ICommandHandler<RemoveLineItemCommand, Result<Order>>
{
    public async ValueTask<Result<Order>> Handle(RemoveLineItemCommand command, CancellationToken cancellationToken)
    {
        var order = await orderRepository.FindByIdAsync(command.OrderId, cancellationToken)
            .ToResultAsync(new Error.NotFound(ResourceRef.For<Order>(command.OrderId)) { Detail = $"Order {command.OrderId.Value} not found." });

        return order.Bind(foundOrder => foundOrder.RemoveLineItem(command.LineItemId));
    }
}
