namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>
/// Cancels an order. Requires the <c>orders:cancel</c> permission plus an ownership check:
/// the actor must be the order's creator, or hold <c>orders:read-all</c> (Admin).
/// </summary>
public sealed record CancelOrderCommand(OrderId OrderId)
    : ICommand<Result<Order>>, IAuthorize, IAuthorizeResource<Order>, IIdentifyResource<Order, OrderId>
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersCancel];

    /// <inheritdoc />
    public OrderId GetResourceId() => OrderId;

    /// <inheritdoc />
    public IResult Authorize(Actor actor, Order resource) =>
        Result.Ensure(
            actor.IsOwner(resource.CreatedByActorId) || actor.HasPermission(Permissions.OrdersReadAll),
            new Error.Forbidden("order.cancel.owner-or-admin", ResourceRef.For<Order>(resource.Id))
            {
                Detail = "Only the order creator or an administrator can cancel this order.",
            });
}

/// <summary>Handler for <see cref="CancelOrderCommand"/>.</summary>
public sealed class CancelOrderCommandHandler : ICommandHandler<CancelOrderCommand, Result<Order>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly TimeProvider _timeProvider;

    /// <summary>Creates the handler.</summary>
    public CancelOrderCommandHandler(
        IOrderRepository orderRepository, IProductRepository productRepository, TimeProvider timeProvider)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async ValueTask<Result<Order>> Handle(CancelOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.FindByIdAsync(command.OrderId, cancellationToken);
        if (!order.HasValue)
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Order>(command.OrderId))
            {
                Detail = $"Order {command.OrderId.Value} not found.",
            });

        var orderValue = order.GetValueOrThrow();
        var productIds = orderValue.LineItems.Select(li => li.ProductId).Distinct().ToArray();
        var products = (await _productRepository.FindManyByIdsAsync(productIds, cancellationToken))
            .ToDictionary(p => p.Id);

        return orderValue.Cancel(products, _timeProvider);
    }
}
