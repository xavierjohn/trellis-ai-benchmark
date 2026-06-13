namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;

public sealed record CreateDraftOrderCommand(CustomerId CustomerId, IReadOnlyList<DraftOrderLine> LineItems)
    : ICommand<Result<Order>>, IAuthorize
{
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersCreate];
}

public sealed record AddLineItemCommand(OrderId OrderId, ProductId ProductId, LineItemQuantity Quantity)
    : ICommand<Result<Order>>, IAuthorize
{
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersCreate];
}

public sealed record RemoveLineItemCommand(OrderId OrderId, LineItemId LineItemId)
    : ICommand<Result<Order>>, IAuthorize
{
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersCreate];
}

public sealed record SubmitOrderCommand(OrderId OrderId) : ICommand<Result<Order>>, IAuthorize
{
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersSubmit];
}

public sealed record ApproveOrderCommand(OrderId OrderId) : ICommand<Result<Order>>, IAuthorize
{
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersApprove];
}

public sealed record ShipOrderCommand(OrderId OrderId) : ICommand<Result<Order>>, IAuthorize
{
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersShip];
}

public sealed record DeliverOrderCommand(OrderId OrderId) : ICommand<Result<Order>>, IAuthorize
{
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersDeliver];
}

public sealed record CancelOrderCommand(OrderId OrderId)
    : ICommand<Result<Order>>, IAuthorize, IAuthorizeResource<Order>, IIdentifyResource<Order, OrderId>
{
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersCancel];

    public OrderId GetResourceId() => OrderId;

    public IResult Authorize(Actor actor, Order resource) =>
        actor.IsOwner(resource.CreatedByActorId) || actor.HasPermission(Permissions.OrdersReadAll)
            ? Result.Ok()
            : Result.Fail(new Error.Forbidden("orders.cancel.owner", ResourceRef.For<Order>(OrderId))
            {
                Detail = "Only the order creator or an administrator can cancel this order.",
            });
}

public sealed class CreateDraftOrderCommandHandler : ICommandHandler<CreateDraftOrderCommand, Result<Order>>
{
    private readonly ICustomerRepository _customers;
    private readonly IProductRepository _products;
    private readonly IOrderRepository _orders;
    private readonly IActorProvider _actorProvider;
    private readonly TimeProvider _timeProvider;

    public CreateDraftOrderCommandHandler(
        ICustomerRepository customers,
        IProductRepository products,
        IOrderRepository orders,
        IActorProvider actorProvider,
        TimeProvider timeProvider)
    {
        _customers = customers;
        _products = products;
        _orders = orders;
        _actorProvider = actorProvider;
        _timeProvider = timeProvider;
    }

    public async ValueTask<Result<Order>> Handle(CreateDraftOrderCommand command, CancellationToken cancellationToken)
    {
        var shape = ValidateRequestedLines(command.LineItems);
        if (shape.IsFailure)
            return Result.Fail<Order>(shape.Error!);

        var customerResult = (await _customers.FindByIdAsync(command.CustomerId, cancellationToken))
            .ToResult(NotFound.Customer(command.CustomerId));
        if (customerResult.IsFailure)
            return Result.Fail<Order>(customerResult.Error!);
        customerResult.TryGetValue(out var customer);

        var products = await _products.FindByIdsAsync(command.LineItems.Select(i => i.ProductId).ToArray(), cancellationToken);
        var missingProductId = command.LineItems.Select(i => i.ProductId).FirstOrDefault(id => products.All(p => p.Id != id));
        if (missingProductId is not null)
            return Result.Fail<Order>(NotFound.Product(missingProductId));

        var actor = (await _actorProvider.GetCurrentActorAsync(cancellationToken))
            .GetValueOrThrow("IAuthorize pipeline guarantees an actor.");
        var orderItems = command.LineItems
            .Select(line => (Product: products.Single(p => p.Id == line.ProductId), line.Quantity))
            .ToArray();

        return Order.TryCreate(customer!, actor.Id, orderItems, _timeProvider)
            .Tap(_orders.Add);
    }

    private static Result<Trellis.Unit> ValidateRequestedLines(IReadOnlyList<DraftOrderLine> lines) =>
        Result.Ensure(lines.Count > 0, Error.InvalidInput.ForField("lineItems", "required", "At least one line item is required."))
            .Ensure(_ => lines.Select(l => l.ProductId).Distinct().Count() == lines.Count,
                Error.InvalidInput.ForField("lineItems", "duplicate_product", "Duplicate product IDs are not allowed."));
}

public sealed class AddLineItemCommandHandler : ICommandHandler<AddLineItemCommand, Result<Order>>
{
    private readonly IOrderRepository _orders;
    private readonly IProductRepository _products;
    private readonly TimeProvider _timeProvider;

    public AddLineItemCommandHandler(IOrderRepository orders, IProductRepository products, TimeProvider timeProvider)
    {
        _orders = orders;
        _products = products;
        _timeProvider = timeProvider;
    }

    public async ValueTask<Result<Order>> Handle(AddLineItemCommand command, CancellationToken cancellationToken)
    {
        var orderResult = (await _orders.FindByIdAsync(command.OrderId, cancellationToken)).ToResult(NotFound.Order(command.OrderId));
        if (orderResult.IsFailure)
            return orderResult;
        orderResult.TryGetValue(out var order);

        var productResult = (await _products.FindByIdAsync(command.ProductId, cancellationToken)).ToResult(NotFound.Product(command.ProductId));
        if (productResult.IsFailure)
            return Result.Fail<Order>(productResult.Error!);
        productResult.TryGetValue(out var product);

        return order!.AddLineItem(product!, command.Quantity, _timeProvider);
    }
}

public sealed class RemoveLineItemCommandHandler : ICommandHandler<RemoveLineItemCommand, Result<Order>>
{
    private readonly IOrderRepository _orders;

    public RemoveLineItemCommandHandler(IOrderRepository orders) => _orders = orders;

    public async ValueTask<Result<Order>> Handle(RemoveLineItemCommand command, CancellationToken cancellationToken) =>
        (await _orders.FindByIdAsync(command.OrderId, cancellationToken))
            .ToResult(NotFound.Order(command.OrderId))
            .Bind(order => order.RemoveLineItem(command.LineItemId));
}

public sealed class SubmitOrderCommandHandler : ICommandHandler<SubmitOrderCommand, Result<Order>>
{
    private readonly IOrderRepository _orders;
    private readonly IProductRepository _products;
    private readonly TimeProvider _timeProvider;

    public SubmitOrderCommandHandler(IOrderRepository orders, IProductRepository products, TimeProvider timeProvider)
    {
        _orders = orders;
        _products = products;
        _timeProvider = timeProvider;
    }

    public async ValueTask<Result<Order>> Handle(SubmitOrderCommand command, CancellationToken cancellationToken)
    {
        var orderResult = (await _orders.FindByIdAsync(command.OrderId, cancellationToken)).ToResult(NotFound.Order(command.OrderId));
        if (orderResult.IsFailure)
            return orderResult;
        orderResult.TryGetValue(out var order);

        if (order!.Status != OrderStatus.Draft)
            return order.Submit(_timeProvider);

        var products = await _products.FindByIdsAsync(order.LineItems.Select(i => i.ProductId).ToArray(), cancellationToken);
        foreach (var item in order.LineItems)
        {
            var product = products.SingleOrDefault(p => p.Id == item.ProductId);
            if (product is null)
                return Result.Fail<Order>(NotFound.Product(item.ProductId));
            if (product.StockQuantity.Value < item.Quantity.Value)
            {
                return Result.Fail<Order>(Error.InvalidInput.ForRule(
                    "products.insufficient_stock",
                    $"Insufficient stock for product {product.Sku.Value}."));
            }
        }

        foreach (var item in order.LineItems)
        {
            var product = products.Single(p => p.Id == item.ProductId);
            var reserveResult = product.ReserveStock(StockAdjustmentQuantity.Create(item.Quantity.Value));
            if (reserveResult.IsFailure)
                return Result.Fail<Order>(reserveResult.Error!);
        }

        return order.Submit(_timeProvider);
    }
}

public sealed class ApproveOrderCommandHandler : ICommandHandler<ApproveOrderCommand, Result<Order>>
{
    private readonly IOrderRepository _orders;
    private readonly TimeProvider _timeProvider;

    public ApproveOrderCommandHandler(IOrderRepository orders, TimeProvider timeProvider)
    {
        _orders = orders;
        _timeProvider = timeProvider;
    }

    public async ValueTask<Result<Order>> Handle(ApproveOrderCommand command, CancellationToken cancellationToken) =>
        (await _orders.FindByIdAsync(command.OrderId, cancellationToken))
            .ToResult(NotFound.Order(command.OrderId))
            .Bind(order => order.Approve(_timeProvider));
}

public sealed class ShipOrderCommandHandler : ICommandHandler<ShipOrderCommand, Result<Order>>
{
    private readonly IOrderRepository _orders;
    private readonly TimeProvider _timeProvider;

    public ShipOrderCommandHandler(IOrderRepository orders, TimeProvider timeProvider)
    {
        _orders = orders;
        _timeProvider = timeProvider;
    }

    public async ValueTask<Result<Order>> Handle(ShipOrderCommand command, CancellationToken cancellationToken) =>
        (await _orders.FindByIdAsync(command.OrderId, cancellationToken))
            .ToResult(NotFound.Order(command.OrderId))
            .Bind(order => order.Ship(_timeProvider));
}

public sealed class DeliverOrderCommandHandler : ICommandHandler<DeliverOrderCommand, Result<Order>>
{
    private readonly IOrderRepository _orders;
    private readonly TimeProvider _timeProvider;

    public DeliverOrderCommandHandler(IOrderRepository orders, TimeProvider timeProvider)
    {
        _orders = orders;
        _timeProvider = timeProvider;
    }

    public async ValueTask<Result<Order>> Handle(DeliverOrderCommand command, CancellationToken cancellationToken) =>
        (await _orders.FindByIdAsync(command.OrderId, cancellationToken))
            .ToResult(NotFound.Order(command.OrderId))
            .Bind(order => order.Deliver(_timeProvider));
}

public sealed class CancelOrderCommandHandler : ICommandHandler<CancelOrderCommand, Result<Order>>
{
    private readonly IOrderRepository _orders;
    private readonly IProductRepository _products;
    private readonly TimeProvider _timeProvider;

    public CancelOrderCommandHandler(IOrderRepository orders, IProductRepository products, TimeProvider timeProvider)
    {
        _orders = orders;
        _products = products;
        _timeProvider = timeProvider;
    }

    public async ValueTask<Result<Order>> Handle(CancelOrderCommand command, CancellationToken cancellationToken)
    {
        var orderResult = (await _orders.FindByIdAsync(command.OrderId, cancellationToken)).ToResult(NotFound.Order(command.OrderId));
        if (orderResult.IsFailure)
            return orderResult;
        orderResult.TryGetValue(out var order);

        var releaseStock = order!.Status.Is(OrderStatus.Submitted, OrderStatus.Approved);
        var cancelResult = order.Cancel(_timeProvider);
        if (cancelResult.IsFailure || !releaseStock)
            return cancelResult;

        var products = await _products.FindByIdsAsync(order.LineItems.Select(i => i.ProductId).ToArray(), cancellationToken);
        foreach (var item in order.LineItems)
        {
            var product = products.SingleOrDefault(p => p.Id == item.ProductId);
            if (product is null)
                return Result.Fail<Order>(NotFound.Product(item.ProductId));
            var addResult = product.AddStock(StockAdjustmentQuantity.Create(item.Quantity.Value));
            if (addResult.IsFailure)
                return Result.Fail<Order>(addResult.Error!);
        }

        return cancelResult;
    }
}
