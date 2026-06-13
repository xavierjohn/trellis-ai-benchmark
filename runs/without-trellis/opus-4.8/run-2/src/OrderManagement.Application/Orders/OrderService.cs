using OrderManagement.Application.Abstractions;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Orders;

namespace OrderManagement.Application.Orders;

public sealed class OrderService
{
    private readonly IOrderRepository _orders;
    private readonly ICustomerRepository _customers;
    private readonly IProductRepository _products;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public OrderService(
        IOrderRepository orders,
        ICustomerRepository customers,
        IProductRepository products,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _orders = orders;
        _customers = customers;
        _products = products;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<Result<OrderDto>> CreateDraftAsync(IActor actor, CreateOrderRequest request, CancellationToken ct = default)
    {
        if (!actor.Has(Permissions.OrdersCreate))
            return Forbidden(Permissions.OrdersCreate);

        if (request.LineItems is null || request.LineItems.Count == 0)
            return Error.Validation("An order must have at least one line item.");

        var customer = await _customers.GetByIdAsync(request.CustomerId, ct);
        if (customer is null)
            return Error.NotFound($"Customer '{request.CustomerId}' was not found.");

        var productIds = request.LineItems.Select(li => li.ProductId).Distinct().ToList();
        var products = await _products.GetByIdsAsync(productIds, ct);
        var productsById = products.ToDictionary(p => p.Id);

        var lineItems = new List<Order.LineItemRequest>();
        foreach (var line in request.LineItems)
        {
            if (!productsById.TryGetValue(line.ProductId, out var product))
                return Error.NotFound($"Product '{line.ProductId}' was not found.");

            lineItems.Add(new Order.LineItemRequest(product.Id, product.ProductName, line.Quantity, product.UnitPrice));
        }

        var orderResult = Order.CreateDraft(request.CustomerId, actor.Id, lineItems, _timeProvider.GetUtcNow());
        if (orderResult.IsFailure)
            return orderResult.Error!;

        await _orders.AddAsync(orderResult.Value, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return orderResult.Value.ToDto();
    }

    public async Task<Result<OrderDto>> AddLineItemAsync(IActor actor, Guid orderId, AddLineItemRequest request, CancellationToken ct = default)
    {
        if (!actor.Has(Permissions.OrdersCreate))
            return Forbidden(Permissions.OrdersCreate);

        var order = await _orders.GetByIdAsync(orderId, ct);
        if (order is null)
            return Error.NotFound($"Order '{orderId}' was not found.");

        var product = await _products.GetByIdAsync(request.ProductId, ct);
        if (product is null)
            return Error.NotFound($"Product '{request.ProductId}' was not found.");

        var result = order.AddLineItem(product.Id, product.ProductName, request.Quantity, product.UnitPrice);
        if (result.IsFailure)
            return result.Error!;

        await _unitOfWork.SaveChangesAsync(ct);
        return order.ToDto();
    }

    public async Task<Result<OrderDto>> RemoveLineItemAsync(IActor actor, Guid orderId, Guid lineItemId, CancellationToken ct = default)
    {
        if (!actor.Has(Permissions.OrdersCreate))
            return Forbidden(Permissions.OrdersCreate);

        var order = await _orders.GetByIdAsync(orderId, ct);
        if (order is null)
            return Error.NotFound($"Order '{orderId}' was not found.");

        var result = order.RemoveLineItem(lineItemId);
        if (result.IsFailure)
            return result.Error!;

        await _unitOfWork.SaveChangesAsync(ct);
        return order.ToDto();
    }

    public async Task<Result<OrderDto>> SubmitAsync(IActor actor, Guid orderId, CancellationToken ct = default)
    {
        if (!actor.Has(Permissions.OrdersSubmit))
            return Forbidden(Permissions.OrdersSubmit);

        var order = await _orders.GetByIdAsync(orderId, ct);
        if (order is null)
            return Error.NotFound($"Order '{orderId}' was not found.");

        var products = await LoadOrderProductsAsync(order, ct);
        var result = order.Submit(products, _timeProvider.GetUtcNow());
        if (result.IsFailure)
            return result.Error!;

        await _unitOfWork.SaveChangesAsync(ct);
        return order.ToDto();
    }

    public async Task<Result<OrderDto>> ApproveAsync(IActor actor, Guid orderId, CancellationToken ct = default)
    {
        if (!actor.Has(Permissions.OrdersApprove))
            return Forbidden(Permissions.OrdersApprove);

        var order = await _orders.GetByIdAsync(orderId, ct);
        if (order is null)
            return Error.NotFound($"Order '{orderId}' was not found.");

        var result = order.Approve(_timeProvider.GetUtcNow());
        if (result.IsFailure)
            return result.Error!;

        await _unitOfWork.SaveChangesAsync(ct);
        return order.ToDto();
    }

    public async Task<Result<OrderDto>> ShipAsync(IActor actor, Guid orderId, CancellationToken ct = default)
    {
        if (!actor.Has(Permissions.OrdersShip))
            return Forbidden(Permissions.OrdersShip);

        var order = await _orders.GetByIdAsync(orderId, ct);
        if (order is null)
            return Error.NotFound($"Order '{orderId}' was not found.");

        var result = order.Ship(_timeProvider.GetUtcNow());
        if (result.IsFailure)
            return result.Error!;

        await _unitOfWork.SaveChangesAsync(ct);
        return order.ToDto();
    }

    public async Task<Result<OrderDto>> DeliverAsync(IActor actor, Guid orderId, CancellationToken ct = default)
    {
        if (!actor.Has(Permissions.OrdersDeliver))
            return Forbidden(Permissions.OrdersDeliver);

        var order = await _orders.GetByIdAsync(orderId, ct);
        if (order is null)
            return Error.NotFound($"Order '{orderId}' was not found.");

        var result = order.Deliver(_timeProvider.GetUtcNow());
        if (result.IsFailure)
            return result.Error!;

        await _unitOfWork.SaveChangesAsync(ct);
        return order.ToDto();
    }

    public async Task<Result<OrderDto>> CancelAsync(IActor actor, Guid orderId, CancellationToken ct = default)
    {
        if (!actor.Has(Permissions.OrdersCancel))
            return Forbidden(Permissions.OrdersCancel);

        var order = await _orders.GetByIdAsync(orderId, ct);
        if (order is null)
            return Error.NotFound($"Order '{orderId}' was not found.");

        // Ownership check: creator, or an actor with orders:read-all (admin).
        var isOwner = string.Equals(order.CreatedByActorId, actor.Id, StringComparison.Ordinal);
        if (!isOwner && !actor.Has(Permissions.OrdersReadAll))
            return Error.Forbidden("Only the order's creator or an administrator may cancel this order.");

        var products = await LoadOrderProductsAsync(order, ct);
        var result = order.Cancel(products, _timeProvider.GetUtcNow());
        if (result.IsFailure)
            return result.Error!;

        await _unitOfWork.SaveChangesAsync(ct);
        return order.ToDto();
    }

    private async Task<IReadOnlyList<Domain.Products.Product>> LoadOrderProductsAsync(Order order, CancellationToken ct)
    {
        var ids = order.LineItems.Select(li => li.ProductId).Distinct().ToList();
        return await _products.GetByIdsAsync(ids, ct);
    }

    private static Error Forbidden(string permission) =>
        Error.Forbidden($"Actor lacks required permission '{permission}'.");
}
