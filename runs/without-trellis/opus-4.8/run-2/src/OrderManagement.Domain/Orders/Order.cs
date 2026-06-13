using OrderManagement.Domain.Common;
using OrderManagement.Domain.Products;

namespace OrderManagement.Domain.Orders;

public sealed class Order
{
    public const int MinQuantity = 1;
    public const int MaxQuantity = 999;

    private readonly List<LineItem> _lineItems = new();
    private readonly List<IDomainEvent> _domainEvents = new();

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

    private Order() { } // EF

    private Order(Guid id, Guid customerId, string createdByActorId, DateTimeOffset createdAt)
    {
        Id = id;
        CustomerId = customerId;
        CreatedByActorId = createdByActorId;
        CreatedAt = createdAt;
        Status = OrderStatus.Draft;
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>A single requested line of a draft order.</summary>
    public readonly record struct LineItemRequest(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice);

    public static Result<Order> CreateDraft(
        Guid customerId,
        string createdByActorId,
        IReadOnlyList<LineItemRequest> lineItems,
        DateTimeOffset createdAt)
    {
        if (customerId == Guid.Empty)
            return Error.Validation("A valid customerId is required.");

        if (lineItems is null || lineItems.Count == 0)
            return Error.Validation("An order must have at least one line item.");

        var duplicateProducts = lineItems
            .GroupBy(li => li.ProductId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (duplicateProducts.Count > 0)
            return Error.Validation("The same product cannot appear in multiple line items. Combine quantities instead.");

        foreach (var li in lineItems)
        {
            if (li.Quantity is < MinQuantity or > MaxQuantity)
                return Error.Validation($"Quantity must be between {MinQuantity} and {MaxQuantity}.");
        }

        var order = new Order(Guid.NewGuid(), customerId, createdByActorId, createdAt);
        foreach (var li in lineItems)
            order._lineItems.Add(new LineItem(Guid.NewGuid(), li.ProductId, li.ProductName, li.Quantity, li.UnitPrice));

        return order;
    }

    public Result AddLineItem(Guid productId, string productName, int quantity, decimal unitPrice)
    {
        if (Status != OrderStatus.Draft)
            return Error.Validation("Line items can only be modified while the order is in Draft status.");

        if (quantity is < MinQuantity or > MaxQuantity)
            return Error.Validation($"Quantity must be between {MinQuantity} and {MaxQuantity}.");

        if (_lineItems.Any(li => li.ProductId == productId))
            return Error.Validation("The product is already in the order. Combine quantities instead.");

        _lineItems.Add(new LineItem(Guid.NewGuid(), productId, productName, quantity, unitPrice));
        return Result.Success();
    }

    public Result RemoveLineItem(Guid lineItemId)
    {
        if (Status != OrderStatus.Draft)
            return Error.Validation("Line items can only be modified while the order is in Draft status.");

        var item = _lineItems.FirstOrDefault(li => li.Id == lineItemId);
        if (item is null)
            return Error.NotFound($"Line item '{lineItemId}' was not found in the order.");

        if (_lineItems.Count <= 1)
            return Error.Validation("Cannot remove the last line item from an order.");

        _lineItems.Remove(item);
        return Result.Success();
    }

    public Result Submit(IEnumerable<Product> products, DateTimeOffset now)
    {
        if (Status != OrderStatus.Draft)
            return InvalidTransition(OrderStatus.Submitted);

        if (_lineItems.Count == 0)
            return Error.Validation("An order must have at least one line item before it can be submitted.");

        var byId = products.ToDictionary(p => p.Id);

        // Validate sufficient stock for every line item before mutating any product.
        foreach (var li in _lineItems)
        {
            if (!byId.TryGetValue(li.ProductId, out var product))
                return Error.Validation($"Product '{li.ProductId}' referenced by the order was not found.");

            if (li.Quantity > product.StockQuantity)
                return Error.Validation(
                    $"Insufficient stock for product '{product.ProductName}' (SKU {product.Sku}): requested {li.Quantity}, available {product.StockQuantity}.");
        }

        // Reserve stock now that all checks passed.
        foreach (var li in _lineItems)
        {
            var reserve = byId[li.ProductId].ReserveStock(li.Quantity);
            if (reserve.IsFailure)
                return reserve.Error!;
        }

        Status = OrderStatus.Submitted;
        SubmittedAt = now;
        _domainEvents.Add(new OrderSubmittedEvent(Id, CustomerId, OrderTotal, now));
        return Result.Success();
    }

    public Result Approve(DateTimeOffset now)
    {
        if (Status != OrderStatus.Submitted)
            return InvalidTransition(OrderStatus.Approved);

        Status = OrderStatus.Approved;
        _domainEvents.Add(new OrderApprovedEvent(Id, now));
        return Result.Success();
    }

    public Result Ship(DateTimeOffset now)
    {
        if (Status != OrderStatus.Approved)
            return InvalidTransition(OrderStatus.Shipped);

        Status = OrderStatus.Shipped;
        ShippedAt = now;
        _domainEvents.Add(new OrderShippedEvent(Id, CustomerId, now));
        return Result.Success();
    }

    public Result Deliver(DateTimeOffset now)
    {
        if (Status != OrderStatus.Shipped)
            return InvalidTransition(OrderStatus.Delivered);

        Status = OrderStatus.Delivered;
        _domainEvents.Add(new OrderDeliveredEvent(Id, now));
        return Result.Success();
    }

    public Result Cancel(IEnumerable<Product> products, DateTimeOffset now)
    {
        if (Status is OrderStatus.Shipped or OrderStatus.Delivered)
            return Error.Validation($"An order in {Status} status cannot be cancelled.");

        if (Status == OrderStatus.Cancelled)
            return Error.Validation("The order is already cancelled.");

        var previousStatus = Status;

        // Release reserved stock if it was reserved (Submitted or Approved).
        if (previousStatus is OrderStatus.Submitted or OrderStatus.Approved)
        {
            var byId = products.ToDictionary(p => p.Id);
            foreach (var li in _lineItems)
            {
                if (byId.TryGetValue(li.ProductId, out var product))
                    product.ReleaseStock(li.Quantity);
            }
        }

        Status = OrderStatus.Cancelled;
        _domainEvents.Add(new OrderCancelledEvent(Id, previousStatus, now));
        return Result.Success();
    }

    private Error InvalidTransition(OrderStatus target) => Error.Validation(
        $"Cannot transition order from {Status} to {target}.");
}
