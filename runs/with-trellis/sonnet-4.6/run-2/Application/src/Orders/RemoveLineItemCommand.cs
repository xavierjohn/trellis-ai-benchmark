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

/// <summary>Handler for RemoveLineItemCommand.</summary>
public sealed class RemoveLineItemCommandHandler : ICommandHandler<RemoveLineItemCommand, Result<Order>>
{
    private readonly IOrderRepository _repository;

    public RemoveLineItemCommandHandler(IOrderRepository repository)
    {
        _repository = repository;
    }

    public async ValueTask<Result<Order>> Handle(RemoveLineItemCommand command, CancellationToken cancellationToken) =>
        await (await _repository.FindByIdAsync(command.OrderId, cancellationToken))
            .ToResult(new Error.NotFound(ResourceRef.For<Order>(command.OrderId)) { Detail = $"Order {command.OrderId} not found." })
            .CheckAsync(order => order.RemoveLineItem(command.LineItemId).AsValueTask());
}
