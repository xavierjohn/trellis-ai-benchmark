namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Application.Products;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>Cancels an order. Only the order creator or users with OrdersReadAll permission can cancel.</summary>
public sealed class CancelOrderCommand : ICommand<Result<Order>>, IAuthorize, IAuthorizeResource<Order>, IIdentifyResource<Order, OrderId>
{
    /// <summary>The ID of the order to cancel.</summary>
    public OrderId OrderId { get; }

    /// <summary>Creates a new CancelOrderCommand.</summary>
    public CancelOrderCommand(OrderId orderId)
    {
        OrderId = orderId;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersCancel];

    /// <inheritdoc />
    public IResult Authorize(Actor actor, Order resource) =>
        Result.Ensure(
            actor.IsOwner(resource.CreatedByActorId) || actor.HasPermission(Permissions.OrdersReadAll),
            new Error.Forbidden("order.cancel.forbidden", ResourceRef.For<Order>(resource.Id))
            { Detail = "Only the order creator or administrators can cancel this order." });

    /// <inheritdoc />
    public OrderId GetResourceId() => OrderId;
}

/// <summary>Handler for CancelOrderCommand.</summary>
public sealed class CancelOrderCommandHandler : ICommandHandler<CancelOrderCommand, Result<Order>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly TimeProvider _timeProvider;

    public CancelOrderCommandHandler(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        TimeProvider timeProvider)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _timeProvider = timeProvider;
    }

    public async ValueTask<Result<Order>> Handle(CancelOrderCommand command, CancellationToken cancellationToken)
    {
        var orderMaybe = await _orderRepository.FindByIdAsync(command.OrderId, cancellationToken);
        if (orderMaybe.HasNoValue)
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Order>(command.OrderId)) { Detail = $"Order {command.OrderId} not found." });

        var order = orderMaybe.GetValueOrThrow();
        var cancelResult = order.Cancel(_timeProvider);
        if (cancelResult.IsFailure)
            return Result.Fail<Order>(cancelResult.Error);

        var cancelValue = cancelResult.GetValueOrThrow();
        if (cancelValue.releaseStock)
        {
            foreach (var (productId, quantity) in cancelValue.lineItems)
            {
                var productMaybe = await _productRepository.FindByIdAsync(productId, cancellationToken);
                if (productMaybe.HasValue)
                    productMaybe.GetValueOrThrow().ReleaseStock(quantity);
            }
        }

        return Result.Ok(order);
    }
}
