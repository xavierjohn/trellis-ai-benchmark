using OrderManagement.Api.Domain.Common;
using OrderManagement.Api.Domain.Products;

namespace OrderManagement.Api.Domain.Orders;

/// <summary>Order aggregate root, including the lifecycle state machine.</summary>
public sealed class Order
{
    public const int MinQuantity = 1;
    public const int MaxQuantity = 999;

    private readonly List<LineItem> _lineItems = new();
    private readonly List<DomainEvent> _events = new();

    private Order() { } // EF

    private Order(Guid id, Guid customerId, string createdByActorId, DateTime createdAt)
    {
        Id = id;
        CustomerId = customerId;
        CreatedByActorId = createdByActorId;
        Status = OrderStatus.Draft;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public string CreatedByActorId { get; private set; } = default!;
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? SubmittedAt { get; private set; }
    public DateTime? ShippedAt { get; private set; }

    public IReadOnlyList<LineItem> LineItems => _lineItems;
    public IReadOnlyList<DomainEvent> Events => _events;

    public decimal OrderTotal => _lineItems.Sum(li => li.LineTotal);

    public void ClearEvents() => _events.Clear();

    public static Order CreateDraft(
        Guid customerId,
        string createdByActorId,
        IReadOnlyList<LineItemDraft> items,
        TimeProvider timeProvider)
    {
        if (items is null || items.Count == 0)
            throw new ValidationAppException("An order must have at least one line item.");

        var duplicateProductIds = items
            .GroupBy(i => i.ProductId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateProductIds.Count > 0)
            throw new ValidationAppException(
                "The same product cannot appear in multiple line items. Combine quantities instead.");

        var order = new Order(Guid.NewGuid(), customerId, createdByActorId, timeProvider.GetUtcNow().UtcDateTime);

        foreach (var item in items)
            order.AddLineItemInternal(item.ProductId, item.ProductName, item.UnitPrice, item.Quantity);

        return order;
    }

    public LineItem AddLineItem(Guid productId, string productName, decimal unitPrice, int quantity)
    {
        EnsureDraft("add a line item");

        if (_lineItems.Any(li => li.ProductId == productId))
            throw new ValidationAppException(
                "The same product cannot appear in multiple line items. Combine quantities instead.");

        return AddLineItemInternal(productId, productName, unitPrice, quantity);
    }

    private LineItem AddLineItemInternal(Guid productId, string productName, decimal unitPrice, int quantity)
    {
        if (quantity < MinQuantity || quantity > MaxQuantity)
            throw new ValidationAppException(
                $"Quantity must be between {MinQuantity} and {MaxQuantity}.",
                new Dictionary<string, string[]> { ["quantity"] = [$"Quantity must be between {MinQuantity} and {MaxQuantity}."] });

        var lineItem = new LineItem(Guid.NewGuid(), productId, productName, quantity, unitPrice);
        _lineItems.Add(lineItem);
        return lineItem;
    }

    public void RemoveLineItem(Guid lineItemId)
    {
        EnsureDraft("remove a line item");

        var lineItem = _lineItems.FirstOrDefault(li => li.Id == lineItemId)
            ?? throw new NotFoundAppException($"Line item '{lineItemId}' was not found on order '{Id}'.");

        if (_lineItems.Count <= 1)
            throw new ValidationAppException("Cannot remove the last line item from an order.");

        _lineItems.Remove(lineItem);
    }

    // ----- State machine -----

    public void Submit(IReadOnlyDictionary<Guid, Product> products, TimeProvider timeProvider)
    {
        if (Status != OrderStatus.Draft)
            throw new ValidationAppException(
                $"Cannot submit an order in status '{Status}'. Only Draft orders can be submitted.");

        if (_lineItems.Count == 0)
            throw new ValidationAppException("An order must have at least one line item before it can be submitted.");

        // Validate sufficient stock for all line items before mutating anything.
        foreach (var li in _lineItems)
        {
            if (!products.TryGetValue(li.ProductId, out var product))
                throw new NotFoundAppException($"Product '{li.ProductId}' was not found.");

            if (li.Quantity > product.StockQuantity)
                throw new ValidationAppException(
                    $"Insufficient stock for product '{product.ProductName}' (SKU {product.Sku}): requested {li.Quantity}, available {product.StockQuantity}.");
        }

        foreach (var li in _lineItems)
            products[li.ProductId].ReserveStock(li.Quantity);

        Status = OrderStatus.Submitted;
        SubmittedAt = timeProvider.GetUtcNow().UtcDateTime;
        _events.Add(new OrderSubmittedEvent(Id, CustomerId, OrderTotal, SubmittedAt.Value));
    }

    public void Approve(TimeProvider timeProvider)
    {
        if (Status != OrderStatus.Submitted)
            throw new ValidationAppException(
                $"Cannot approve an order in status '{Status}'. Only Submitted orders can be approved.");

        Status = OrderStatus.Approved;
        _events.Add(new OrderApprovedEvent(Id, timeProvider.GetUtcNow().UtcDateTime));
    }

    public void Ship(TimeProvider timeProvider)
    {
        if (Status != OrderStatus.Approved)
            throw new ValidationAppException(
                $"Cannot ship an order in status '{Status}'. Only Approved orders can be shipped.");

        Status = OrderStatus.Shipped;
        ShippedAt = timeProvider.GetUtcNow().UtcDateTime;
        _events.Add(new OrderShippedEvent(Id, CustomerId, ShippedAt.Value));
    }

    public void Deliver(TimeProvider timeProvider)
    {
        if (Status != OrderStatus.Shipped)
            throw new ValidationAppException(
                $"Cannot deliver an order in status '{Status}'. Only Shipped orders can be delivered.");

        Status = OrderStatus.Delivered;
        _events.Add(new OrderDeliveredEvent(Id, timeProvider.GetUtcNow().UtcDateTime));
    }

    public void Cancel(IReadOnlyDictionary<Guid, Product> products, TimeProvider timeProvider)
    {
        if (Status is OrderStatus.Shipped or OrderStatus.Delivered)
            throw new ValidationAppException(
                $"Cannot cancel an order in status '{Status}'. Shipped or Delivered orders cannot be cancelled.");

        if (Status == OrderStatus.Cancelled)
            throw new ValidationAppException("Order is already cancelled.");

        var fromStatus = Status;

        // Release reserved stock if it had been reserved (Submitted or Approved).
        if (fromStatus is OrderStatus.Submitted or OrderStatus.Approved)
        {
            foreach (var li in _lineItems)
            {
                if (products.TryGetValue(li.ProductId, out var product))
                    product.ReleaseStock(li.Quantity);
            }
        }

        Status = OrderStatus.Cancelled;
        _events.Add(new OrderCancelledEvent(Id, fromStatus, timeProvider.GetUtcNow().UtcDateTime));
    }

    private void EnsureDraft(string action)
    {
        if (Status != OrderStatus.Draft)
            throw new ValidationAppException($"Cannot {action} on an order in status '{Status}'. Only Draft orders can be modified.");
    }
}

/// <summary>Snapshot of a product captured when building a line item.</summary>
public readonly record struct LineItemDraft(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity);
