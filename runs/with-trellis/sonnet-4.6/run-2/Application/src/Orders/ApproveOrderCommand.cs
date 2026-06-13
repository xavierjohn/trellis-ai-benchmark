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

/// <summary>Handler for ApproveOrderCommand.</summary>
public sealed class ApproveOrderCommandHandler : ICommandHandler<ApproveOrderCommand, Result<Order>>
{
    private readonly IOrderRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ApproveOrderCommandHandler(IOrderRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async ValueTask<Result<Order>> Handle(ApproveOrderCommand command, CancellationToken cancellationToken) =>
        await (await _repository.FindByIdAsync(command.OrderId, cancellationToken))
            .ToResult(new Error.NotFound(ResourceRef.For<Order>(command.OrderId)) { Detail = $"Order {command.OrderId} not found." })
            .CheckAsync(order => order.Approve(_timeProvider).AsValueTask());
}
