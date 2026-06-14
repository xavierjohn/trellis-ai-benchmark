namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>Approves a submitted order.</summary>
public sealed record ApproveOrderCommand(OrderId OrderId) : ICommand<Result<Order>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersApprove];
}

/// <summary>Ships an approved order.</summary>
public sealed record ShipOrderCommand(OrderId OrderId) : ICommand<Result<Order>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersShip];
}

/// <summary>Marks a shipped order as delivered.</summary>
public sealed record DeliverOrderCommand(OrderId OrderId) : ICommand<Result<Order>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersDeliver];
}

/// <summary>Handler for <see cref="ApproveOrderCommand"/>.</summary>
public sealed class ApproveOrderCommandHandler : ICommandHandler<ApproveOrderCommand, Result<Order>>
{
    private readonly IOrderRepository _repository;
    private readonly TimeProvider _timeProvider;

    /// <summary>Creates the handler.</summary>
    public ApproveOrderCommandHandler(IOrderRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async ValueTask<Result<Order>> Handle(ApproveOrderCommand command, CancellationToken cancellationToken) =>
        await _repository.FindByIdAsync(command.OrderId, cancellationToken)
            .ToResultAsync(new Error.NotFound(ResourceRef.For<Order>(command.OrderId))
            {
                Detail = $"Order {command.OrderId.Value} not found.",
            })
            .BindAsync(order => order.Approve(_timeProvider));
}

/// <summary>Handler for <see cref="ShipOrderCommand"/>.</summary>
public sealed class ShipOrderCommandHandler : ICommandHandler<ShipOrderCommand, Result<Order>>
{
    private readonly IOrderRepository _repository;
    private readonly TimeProvider _timeProvider;

    /// <summary>Creates the handler.</summary>
    public ShipOrderCommandHandler(IOrderRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async ValueTask<Result<Order>> Handle(ShipOrderCommand command, CancellationToken cancellationToken) =>
        await _repository.FindByIdAsync(command.OrderId, cancellationToken)
            .ToResultAsync(new Error.NotFound(ResourceRef.For<Order>(command.OrderId))
            {
                Detail = $"Order {command.OrderId.Value} not found.",
            })
            .BindAsync(order => order.Ship(_timeProvider));
}

/// <summary>Handler for <see cref="DeliverOrderCommand"/>.</summary>
public sealed class DeliverOrderCommandHandler : ICommandHandler<DeliverOrderCommand, Result<Order>>
{
    private readonly IOrderRepository _repository;
    private readonly TimeProvider _timeProvider;

    /// <summary>Creates the handler.</summary>
    public DeliverOrderCommandHandler(IOrderRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async ValueTask<Result<Order>> Handle(DeliverOrderCommand command, CancellationToken cancellationToken) =>
        await _repository.FindByIdAsync(command.OrderId, cancellationToken)
            .ToResultAsync(new Error.NotFound(ResourceRef.For<Order>(command.OrderId))
            {
                Detail = $"Order {command.OrderId.Value} not found.",
            })
            .BindAsync(order => order.Deliver(_timeProvider));
}
