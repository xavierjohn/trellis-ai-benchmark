using OrderManagement.Api.Application.Abstractions;
using OrderManagement.Api.Application.Auth;
using OrderManagement.Api.Contracts;
using OrderManagement.Api.Domain.Common;
using OrderManagement.Api.Domain.Orders;
using OrderManagement.Api.Domain.Products;

namespace OrderManagement.Api.Application.Orders;

public sealed class OrderService
{
    private readonly IOrderRepository _orders;
    private readonly IProductRepository _products;
    private readonly ICustomerRepository _customers;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _time;

    public OrderService(
        IOrderRepository orders,
        IProductRepository products,
        ICustomerRepository customers,
        IUnitOfWork uow,
        TimeProvider time)
    {
        _orders = orders;
        _products = products;
        _customers = customers;
        _uow = uow;
        _time = time;
    }

    public async Task<Order> CreateDraftAsync(Actor actor, CreateOrderRequest request, CancellationToken ct = default)
    {
        actor.Require(Permissions.OrdersCreate);

        if (request.CustomerId == Guid.Empty)
            throw new ValidationAppException("CustomerId is required.");

        var items = request.Items ?? new List<CreateOrderLineItemDto>();
        if (items.Count == 0)
            throw new ValidationAppException("An order must have at least one line item.");

        var duplicates = items.GroupBy(i => i.ProductId).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicates.Count > 0)
            throw new ValidationAppException(
                "The same product cannot appear in multiple line items. Combine quantities instead.");

        foreach (var item in items)
        {
            if (item.Quantity < Order.MinQuantity || item.Quantity > Order.MaxQuantity)
                throw new ValidationAppException(
                    $"Quantity must be between {Order.MinQuantity} and {Order.MaxQuantity}.");
        }

        var customer = await _customers.GetByIdAsync(request.CustomerId, ct)
            ?? throw new NotFoundAppException($"Customer '{request.CustomerId}' was not found.");

        var productIds = items.Select(i => i.ProductId).ToList();
        var products = await _products.GetByIdsAsync(productIds, ct);
        var productMap = products.ToDictionary(p => p.Id);

        var drafts = new List<LineItemDraft>();
        foreach (var item in items)
        {
            if (!productMap.TryGetValue(item.ProductId, out var product))
                throw new NotFoundAppException($"Product '{item.ProductId}' was not found.");

            drafts.Add(new LineItemDraft(product.Id, product.ProductName, product.UnitPrice, item.Quantity));
        }

        var order = Order.CreateDraft(customer.Id, actor.Id, drafts, _time);
        await _orders.AddAsync(order, ct);
        await _uow.SaveChangesAsync(ct);
        return order;
    }

    public async Task<Order> AddLineItemAsync(Actor actor, Guid orderId, AddLineItemRequest request, CancellationToken ct = default)
    {
        actor.Require(Permissions.OrdersCreate);

        var order = await GetRequiredOrderAsync(orderId, ct);

        var product = await _products.GetByIdAsync(request.ProductId, ct)
            ?? throw new NotFoundAppException($"Product '{request.ProductId}' was not found.");

        order.AddLineItem(product.Id, product.ProductName, product.UnitPrice, request.Quantity);
        var added = order.LineItems.Single(li => li.ProductId == product.Id);
        _orders.TrackAddedLineItem(added);
        await _uow.SaveChangesAsync(ct);
        return order;
    }

    public async Task<Order> RemoveLineItemAsync(Actor actor, Guid orderId, Guid lineItemId, CancellationToken ct = default)
    {
        actor.Require(Permissions.OrdersCreate);

        var order = await GetRequiredOrderAsync(orderId, ct);
        order.RemoveLineItem(lineItemId);
        await _uow.SaveChangesAsync(ct);
        return order;
    }

    public async Task<Order> SubmitAsync(Actor actor, Guid orderId, CancellationToken ct = default)
    {
        actor.Require(Permissions.OrdersSubmit);

        var order = await GetRequiredOrderAsync(orderId, ct);
        var productMap = await LoadProductsForOrderAsync(order, ct);

        order.Submit(productMap, _time);
        await _uow.SaveChangesAsync(ct);
        return order;
    }

    public async Task<Order> ApproveAsync(Actor actor, Guid orderId, CancellationToken ct = default)
    {
        actor.Require(Permissions.OrdersApprove);

        var order = await GetRequiredOrderAsync(orderId, ct);
        order.Approve(_time);
        await _uow.SaveChangesAsync(ct);
        return order;
    }

    public async Task<Order> ShipAsync(Actor actor, Guid orderId, CancellationToken ct = default)
    {
        actor.Require(Permissions.OrdersShip);

        var order = await GetRequiredOrderAsync(orderId, ct);
        order.Ship(_time);
        await _uow.SaveChangesAsync(ct);
        return order;
    }

    public async Task<Order> DeliverAsync(Actor actor, Guid orderId, CancellationToken ct = default)
    {
        actor.Require(Permissions.OrdersDeliver);

        var order = await GetRequiredOrderAsync(orderId, ct);
        order.Deliver(_time);
        await _uow.SaveChangesAsync(ct);
        return order;
    }

    public async Task<Order> CancelAsync(Actor actor, Guid orderId, CancellationToken ct = default)
    {
        actor.Require(Permissions.OrdersCancel);

        var order = await GetRequiredOrderAsync(orderId, ct);

        // Ownership check: creator OR admin (has orders:read-all).
        var isOwner = string.Equals(order.CreatedByActorId, actor.Id, StringComparison.Ordinal);
        var isAdmin = actor.Has(Permissions.OrdersReadAll);
        if (!isOwner && !isAdmin)
            throw new ForbiddenAppException("Only the order creator or an administrator can cancel this order.");

        // Stock was only reserved once submitted/approved; load products only then.
        var productMap = order.Status is OrderStatus.Submitted or OrderStatus.Approved
            ? await LoadProductsForOrderAsync(order, ct)
            : new Dictionary<Guid, Product>();

        order.Cancel(productMap, _time);
        await _uow.SaveChangesAsync(ct);
        return order;
    }

    public async Task<Order> GetByIdAsync(Actor actor, Guid orderId, CancellationToken ct = default)
    {
        actor.Require(Permissions.OrdersRead);
        return await GetRequiredOrderAsync(orderId, ct);
    }

    public async Task<IReadOnlyList<Order>> ListByCustomerAsync(Actor actor, Guid customerId, CancellationToken ct = default)
    {
        actor.Require(Permissions.OrdersReadAll);

        _ = await _customers.GetByIdAsync(customerId, ct)
            ?? throw new NotFoundAppException($"Customer '{customerId}' was not found.");

        return await _orders.GetByCustomerAsync(customerId, ct);
    }

    public async Task<IReadOnlyList<Order>> ListOverdueAsync(Actor actor, CancellationToken ct = default)
    {
        actor.Require(Permissions.OrdersReadAll);

        var now = _time.GetUtcNow().UtcDateTime;
        var cutoff = now.AddDays(-OverdueOrderSpecification.OverdueDays);
        var candidates = await _orders.GetSubmittedBeforeAsync(cutoff, ct);
        return candidates.Where(o => OverdueOrderSpecification.IsOverdue(o, now)).ToList();
    }

    private async Task<Order> GetRequiredOrderAsync(Guid orderId, CancellationToken ct) =>
        await _orders.GetByIdAsync(orderId, ct)
        ?? throw new NotFoundAppException($"Order '{orderId}' was not found.");

    private async Task<Dictionary<Guid, Product>> LoadProductsForOrderAsync(Order order, CancellationToken ct)
    {
        var ids = order.LineItems.Select(li => li.ProductId).Distinct().ToList();
        var products = await _products.GetByIdsAsync(ids, ct);
        var map = products.ToDictionary(p => p.Id);

        foreach (var li in order.LineItems)
        {
            if (!map.ContainsKey(li.ProductId))
                throw new NotFoundAppException($"Product '{li.ProductId}' referenced by the order was not found.");
        }

        return map;
    }
}
