namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>Removes a line item from a draft order.</summary>
public sealed record RemoveLineItemCommand(OrderId OrderId, LineItemId LineItemId) : ICommand<Result<Order>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersCreate];
}

/// <summary>Handler for <see cref="RemoveLineItemCommand"/>.</summary>
public sealed class RemoveLineItemCommandHandler : ICommandHandler<RemoveLineItemCommand, Result<Order>>
{
    private readonly IOrderRepository _orderRepository;

    /// <summary>Creates the handler.</summary>
    public RemoveLineItemCommandHandler(IOrderRepository orderRepository) => _orderRepository = orderRepository;

    /// <inheritdoc />
    public async ValueTask<Result<Order>> Handle(RemoveLineItemCommand command, CancellationToken cancellationToken) =>
        await _orderRepository.FindByIdAsync(command.OrderId, cancellationToken)
            .ToResultAsync(new Error.NotFound(ResourceRef.For<Order>(command.OrderId))
            {
                Detail = $"Order {command.OrderId.Value} not found.",
            })
            .BindAsync(order => order.RemoveLineItem(command.LineItemId));
}
