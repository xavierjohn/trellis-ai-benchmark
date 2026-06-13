namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>Ships an approved order.</summary>
public sealed record ShipOrderCommand(OrderId OrderId) : ICommand<Result<Order>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersShip];
}

/// <summary>Handler for ShipOrderCommand.</summary>
public sealed class ShipOrderCommandHandler : ICommandHandler<ShipOrderCommand, Result<Order>>
{
    private readonly IOrderRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ShipOrderCommandHandler(IOrderRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async ValueTask<Result<Order>> Handle(ShipOrderCommand command, CancellationToken cancellationToken) =>
        await (await _repository.FindByIdAsync(command.OrderId, cancellationToken))
            .ToResult(new Error.NotFound(ResourceRef.For<Order>(command.OrderId)) { Detail = $"Order {command.OrderId} not found." })
            .CheckAsync(order => order.Ship(_timeProvider).AsValueTask());
}
