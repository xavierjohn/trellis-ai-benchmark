using OrderManagement.Domain.Common;
using OrderManagement.Domain.Products;

namespace OrderManagement.Domain.Orders;

public sealed class Order
{
    public const int MinQuantity = 1;
    public const int MaxQuantity = 999;

    private readonly List<LineItem> _lineItems = [];
    private readonly List<IDomainEvent> _domainEvents = [];

    private Order(Guid id, Guid customerId, string createdByActorId, DateTimeOffset createdAt)
    {
        Id = id;
        CustomerId = customerId;
        CreatedByActorId = createdByActorId;
        Status = OrderStatus.Draft;
        CreatedAt = createdAt;
    }

    // Parameterless ctor for EF Core materialization.
    private Order() { }

    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public string CreatedByActorId { get; private set; } = string.Empty;
    public OrderStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? SubmittedAt { get; private set; }
    public DateTimeOffset? ShippedAt { get; private set; }

    public IReadOnlyList<LineItem> LineItems => _lineItems;
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents;

    public decimal OrderTotal => _lineItems.Sum(li => li.LineTotal);

    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>A request for one product/quantity pair, used when creating an order.</summary>
    public readonly record struct LineRequest(Guid ProductId, int Quantity);

    public static Result<Order> CreateDraft(
        Guid customerId,
        string createdByActorId,
        IReadOnlyList<(LineRequest Request, Product Product)> lines,
        DateTimeOffset createdAt)
    {
        if (lines.Count == 0)
            return Error.Validation("An order must have at least one line item.");

        var seenProducts = new HashSet<Guid>();
        foreach (var (request, _) in lines)
        {
            if (request.Quantity < MinQuantity || request.Quantity > MaxQuantity)
                return Error.Validation($"Quantity must be between {MinQuantity} and {MaxQuantity}.");

            if (!seenProducts.Add(request.ProductId))
                return Error.Validation("The same product cannot appear in multiple line items. Combine quantities instead.");
        }

        var order = new Order(Guid.NewGuid(), customerId, createdByActorId, createdAt);
        foreach (var (request, product) in lines)
        {
            order._lineItems.Add(new LineItem(
                Guid.NewGuid(), product.Id, product.ProductName, request.Quantity, product.UnitPrice));
        }

        return order;
    }

    public Result<LineItem> AddLineItem(Product product, int quantity)
    {
        if (Status != OrderStatus.Draft)
            return Error.Validation("Line items can only be modified while the order is in Draft status.");

        if (quantity < MinQuantity || quantity > MaxQuantity)
            return Error.Validation($"Quantity must be between {MinQuantity} and {MaxQuantity}.");

        if (_lineItems.Any(li => li.ProductId == product.Id))
            return Error.Validation("The same product cannot appear in multiple line items. Combine quantities instead.");

        var item = new LineItem(Guid.NewGuid(), product.Id, product.ProductName, quantity, product.UnitPrice);
        _lineItems.Add(item);
        return item;
    }

    public Result RemoveLineItem(Guid lineItemId)
    {
        if (Status != OrderStatus.Draft)
            return Error.Validation("Line items can only be modified while the order is in Draft status.");

        var item = _lineItems.FirstOrDefault(li => li.Id == lineItemId);
        if (item is null)
            return Error.NotFound($"Line item '{lineItemId}' was not found in order '{Id}'.");

        if (_lineItems.Count <= 1)
            return Error.Validation("Cannot remove the last line item from an order.");

        _lineItems.Remove(item);
        return Result.Success();
    }

    /// <summary>Draft → Submitted. Reserves stock for every line item.</summary>
    public Result Submit(IReadOnlyDictionary<Guid, Product> products, DateTimeOffset now)
    {
        if (Status != OrderStatus.Draft)
            return InvalidTransition(OrderStatus.Submitted);

        if (_lineItems.Count == 0)
            return Error.Validation("An order must have at least one line item before submission.");

        // Verify availability for all items before mutating any stock.
        foreach (var item in _lineItems)
        {
            if (!products.TryGetValue(item.ProductId, out var product))
                return Error.NotFound($"Product '{item.ProductId}' referenced by the order was not found.");

            if (item.Quantity > product.StockQuantity)
                return Error.Validation($"Insufficient stock for product '{product.ProductName}' (SKU {product.Sku}). Requested {item.Quantity}, available {product.StockQuantity}.");
        }

        foreach (var item in _lineItems)
        {
            var reserve = products[item.ProductId].ReserveStock(item.Quantity);
            if (reserve.IsFailure)
                return reserve.Error!;
        }

        Status = OrderStatus.Submitted;
        SubmittedAt = now;
        _domainEvents.Add(new OrderSubmittedEvent(Id, CustomerId, OrderTotal, now));
        return Result.Success();
    }

    /// <summary>Submitted → Approved.</summary>
    public Result Approve(DateTimeOffset now)
    {
        if (Status != OrderStatus.Submitted)
            return InvalidTransition(OrderStatus.Approved);

        Status = OrderStatus.Approved;
        _domainEvents.Add(new OrderApprovedEvent(Id, now));
        return Result.Success();
    }

    /// <summary>Approved → Shipped.</summary>
    public Result Ship(DateTimeOffset now)
    {
        if (Status != OrderStatus.Approved)
            return InvalidTransition(OrderStatus.Shipped);

        Status = OrderStatus.Shipped;
        ShippedAt = now;
        _domainEvents.Add(new OrderShippedEvent(Id, CustomerId, now));
        return Result.Success();
    }

    /// <summary>Shipped → Delivered.</summary>
    public Result Deliver(DateTimeOffset now)
    {
        if (Status != OrderStatus.Shipped)
            return InvalidTransition(OrderStatus.Delivered);

        Status = OrderStatus.Delivered;
        _domainEvents.Add(new OrderDeliveredEvent(Id, now));
        return Result.Success();
    }

    /// <summary>Draft/Submitted/Approved → Cancelled. Releases reserved stock when applicable.</summary>
    public Result Cancel(IReadOnlyDictionary<Guid, Product> products, DateTimeOffset now)
    {
        if (Status is OrderStatus.Shipped or OrderStatus.Delivered or OrderStatus.Cancelled)
            return InvalidTransition(OrderStatus.Cancelled);

        var cancelledFrom = Status;

        if (cancelledFrom is OrderStatus.Submitted or OrderStatus.Approved)
        {
            foreach (var item in _lineItems)
            {
                if (products.TryGetValue(item.ProductId, out var product))
                    product.ReleaseStock(item.Quantity);
            }
        }

        Status = OrderStatus.Cancelled;
        _domainEvents.Add(new OrderCancelledEvent(Id, cancelledFrom, now));
        return Result.Success();
    }

    private Error InvalidTransition(OrderStatus target)
        => Error.Validation($"Cannot transition order from {Status} to {target}.");
}
