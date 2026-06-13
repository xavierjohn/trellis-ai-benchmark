using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Authorization;
using OrderManagement.Application.Contracts;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Products;

namespace OrderManagement.Application.Orders;

public sealed class OrderService
{
    private readonly IOrderRepository _orders;
    private readonly ICustomerRepository _customers;
    private readonly IProductRepository _products;
    private readonly IUnitOfWork _uow;
    private readonly IActorProvider _actorProvider;
    private readonly TimeProvider _timeProvider;

    public OrderService(
        IOrderRepository orders,
        ICustomerRepository customers,
        IProductRepository products,
        IUnitOfWork uow,
        IActorProvider actorProvider,
        TimeProvider timeProvider)
    {
        _orders = orders;
        _customers = customers;
        _products = products;
        _uow = uow;
        _actorProvider = actorProvider;
        _timeProvider = timeProvider;
    }

    private DateTimeOffset Now => _timeProvider.GetUtcNow();

    // 6.4 Create Draft Order
    public async Task<Result<OrderResponse>> CreateAsync(CreateOrderRequest request, CancellationToken ct = default)
    {
        var actor = _actorProvider.Current;
        var auth = AuthorizationGuard.Require(actor, Permissions.OrdersCreate);
        if (auth.IsFailure) return auth.Error!;

        if (request.CustomerId == Guid.Empty)
            return Error.Validation(nameof(request.CustomerId), "CustomerId is required.");

        var lines = request.Lines;
        if (lines is null || lines.Count == 0)
            return Error.Validation(nameof(request.Lines), "An order must have at least one line item.");

        if (lines.Select(l => l.ProductId).Distinct().Count() != lines.Count)
            return Error.Validation(nameof(request.Lines),
                "The same product cannot appear in multiple line items. Combine quantities instead.");

        var customer = await _customers.GetByIdAsync(request.CustomerId, ct);
        if (customer is null)
            return Error.NotFound($"Customer '{request.CustomerId}' was not found.");

        var productIds = lines.Select(l => l.ProductId).ToList();
        var products = await _products.GetByIdsAsync(productIds, ct);
        var productsById = products.ToDictionary(p => p.Id);

        var newLines = new List<Order.NewLineItem>();
        foreach (var line in lines)
        {
            if (!productsById.TryGetValue(line.ProductId, out var product))
                return Error.NotFound($"Product '{line.ProductId}' was not found.");

            newLines.Add(new Order.NewLineItem(product.Id, product.ProductName, line.Quantity, product.UnitPrice));
        }

        var orderResult = Order.Create(request.CustomerId, actor.Id, newLines, Now);
        if (orderResult.IsFailure) return orderResult.Error!;

        var order = orderResult.Value;
        await _orders.AddAsync(order, ct);
        await _uow.SaveChangesAsync(ct);

        return OrderResponse.From(order);
    }

    // 6.5 Add Line Item
    public async Task<Result<OrderResponse>> AddLineItemAsync(Guid orderId, AddLineItemRequest request, CancellationToken ct = default)
    {
        var auth = AuthorizationGuard.Require(_actorProvider.Current, Permissions.OrdersCreate);
        if (auth.IsFailure) return auth.Error!;

        var order = await _orders.GetByIdAsync(orderId, ct);
        if (order is null)
            return Error.NotFound($"Order '{orderId}' was not found.");

        var product = await _products.GetByIdAsync(request.ProductId, ct);
        if (product is null)
            return Error.NotFound($"Product '{request.ProductId}' was not found.");

        var result = order.AddLineItem(product.Id, product.ProductName, request.Quantity, product.UnitPrice);
        if (result.IsFailure) return result.Error!;

        await _uow.SaveChangesAsync(ct);
        return OrderResponse.From(order);
    }

    // 6.6 Remove Line Item
    public async Task<Result<OrderResponse>> RemoveLineItemAsync(Guid orderId, Guid lineItemId, CancellationToken ct = default)
    {
        var auth = AuthorizationGuard.Require(_actorProvider.Current, Permissions.OrdersCreate);
        if (auth.IsFailure) return auth.Error!;

        var order = await _orders.GetByIdAsync(orderId, ct);
        if (order is null)
            return Error.NotFound($"Order '{orderId}' was not found.");

        var result = order.RemoveLineItem(lineItemId);
        if (result.IsFailure) return result.Error!;

        await _uow.SaveChangesAsync(ct);
        return OrderResponse.From(order);
    }

    // 6.7 Submit Order
    public async Task<Result<OrderResponse>> SubmitAsync(Guid orderId, CancellationToken ct = default)
    {
        var auth = AuthorizationGuard.Require(_actorProvider.Current, Permissions.OrdersSubmit);
        if (auth.IsFailure) return auth.Error!;

        var order = await _orders.GetByIdAsync(orderId, ct);
        if (order is null)
            return Error.NotFound($"Order '{orderId}' was not found.");

        var productsById = await LoadProductsForOrderAsync(order, ct);

        var result = order.Submit(productsById, Now);
        if (result.IsFailure) return result.Error!;

        await _uow.SaveChangesAsync(ct);
        return OrderResponse.From(order);
    }

    // 6.8 Approve Order
    public async Task<Result<OrderResponse>> ApproveAsync(Guid orderId, CancellationToken ct = default)
        => await TransitionAsync(orderId, Permissions.OrdersApprove, o => o.Approve(Now), ct);

    // 6.9 Ship Order
    public async Task<Result<OrderResponse>> ShipAsync(Guid orderId, CancellationToken ct = default)
        => await TransitionAsync(orderId, Permissions.OrdersShip, o => o.Ship(Now), ct);

    // 6.10 Deliver Order
    public async Task<Result<OrderResponse>> DeliverAsync(Guid orderId, CancellationToken ct = default)
        => await TransitionAsync(orderId, Permissions.OrdersDeliver, o => o.Deliver(Now), ct);

    // 6.11 Cancel Order (with ownership check)
    public async Task<Result<OrderResponse>> CancelAsync(Guid orderId, CancellationToken ct = default)
    {
        var actor = _actorProvider.Current;
        var auth = AuthorizationGuard.Require(actor, Permissions.OrdersCancel);
        if (auth.IsFailure) return auth.Error!;

        var order = await _orders.GetByIdAsync(orderId, ct);
        if (order is null)
            return Error.NotFound($"Order '{orderId}' was not found.");

        var isOwner = string.Equals(order.CreatedByActorId, actor.Id, StringComparison.Ordinal);
        if (!isOwner && !actor.Has(Permissions.OrdersReadAll))
            return Error.Forbidden("Only the order creator or an administrator may cancel this order.");

        var productsById = await LoadProductsForOrderAsync(order, ct);

        var result = order.Cancel(productsById, Now);
        if (result.IsFailure) return result.Error!;

        await _uow.SaveChangesAsync(ct);
        return OrderResponse.From(order);
    }

    // 6.12 Get Order by ID
    public async Task<Result<OrderResponse>> GetByIdAsync(Guid orderId, CancellationToken ct = default)
    {
        var auth = AuthorizationGuard.Require(_actorProvider.Current, Permissions.OrdersRead);
        if (auth.IsFailure) return auth.Error!;

        var order = await _orders.GetByIdAsync(orderId, ct);
        if (order is null)
            return Error.NotFound($"Order '{orderId}' was not found.");

        return OrderResponse.From(order);
    }

    // 6.13 List Orders by Customer
    public async Task<Result<IReadOnlyList<OrderResponse>>> ListByCustomerAsync(Guid customerId, CancellationToken ct = default)
    {
        var auth = AuthorizationGuard.Require(_actorProvider.Current, Permissions.OrdersReadAll);
        if (auth.IsFailure) return auth.Error!;

        var customer = await _customers.GetByIdAsync(customerId, ct);
        if (customer is null)
            return Error.NotFound($"Customer '{customerId}' was not found.");

        var orders = await _orders.GetByCustomerAsync(customerId, ct);
        return Result<IReadOnlyList<OrderResponse>>.Success(orders.Select(OrderResponse.From).ToList());
    }

    // 6.14 List Overdue Orders
    public async Task<Result<IReadOnlyList<OrderResponse>>> ListOverdueAsync(CancellationToken ct = default)
    {
        var auth = AuthorizationGuard.Require(_actorProvider.Current, Permissions.OrdersReadAll);
        if (auth.IsFailure) return auth.Error!;

        var now = Now;
        var submitted = await _orders.GetSubmittedAsync(ct);
        var overdue = submitted
            .Where(o => OverdueOrderSpecification.IsOverdue(o, now))
            .Select(OrderResponse.From)
            .ToList();

        return Result<IReadOnlyList<OrderResponse>>.Success(overdue);
    }

    private async Task<Result<OrderResponse>> TransitionAsync(
        Guid orderId, string permission, Func<Order, Result> transition, CancellationToken ct)
    {
        var auth = AuthorizationGuard.Require(_actorProvider.Current, permission);
        if (auth.IsFailure) return auth.Error!;

        var order = await _orders.GetByIdAsync(orderId, ct);
        if (order is null)
            return Error.NotFound($"Order '{orderId}' was not found.");

        var result = transition(order);
        if (result.IsFailure) return result.Error!;

        await _uow.SaveChangesAsync(ct);
        return OrderResponse.From(order);
    }

    private async Task<IReadOnlyDictionary<Guid, Product>> LoadProductsForOrderAsync(Order order, CancellationToken ct)
    {
        var productIds = order.LineItems.Select(li => li.ProductId).Distinct().ToList();
        var products = await _products.GetByIdsAsync(productIds, ct);
        return products.ToDictionary(p => p.Id);
    }
}
