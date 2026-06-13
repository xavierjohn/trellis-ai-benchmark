namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>Marks an order as delivered.</summary>
public sealed record DeliverOrderCommand(OrderId OrderId) : ICommand<Result<Order>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersDeliver];
}

/// <summary>Handler for DeliverOrderCommand.</summary>
public sealed class DeliverOrderCommandHandler : ICommandHandler<DeliverOrderCommand, Result<Order>>
{
    private readonly IOrderRepository _repository;
    private readonly TimeProvider _timeProvider;

    public DeliverOrderCommandHandler(IOrderRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async ValueTask<Result<Order>> Handle(DeliverOrderCommand command, CancellationToken cancellationToken) =>
        await (await _repository.FindByIdAsync(command.OrderId, cancellationToken))
            .ToResult(new Error.NotFound(ResourceRef.For<Order>(command.OrderId)) { Detail = $"Order {command.OrderId} not found." })
            .CheckAsync(order => order.Deliver(_timeProvider).AsValueTask());
}
