using OrderManagement.Domain.Common;
using OrderManagement.Domain.Products;

namespace OrderManagement.Domain.Orders;

/// <summary>Order aggregate root, including its line items and lifecycle state machine.</summary>
public sealed class Order
{
    private readonly List<LineItem> _lineItems = new();
    private readonly List<IDomainEvent> _domainEvents = new();

    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public string CreatedByActorId { get; private set; } = null!;
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
        Status = OrderStatus.Draft;
        CreatedAt = createdAt;
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>Describes a line item to add at creation time, with the price snapshot already resolved.</summary>
    public readonly record struct NewLineItem(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice);

    public static Result<Order> Create(
        Guid customerId,
        string createdByActorId,
        IReadOnlyList<NewLineItem> lineItems,
        DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(createdByActorId))
            return Error.Validation(nameof(createdByActorId), "Creating actor is required.");

        if (lineItems is null || lineItems.Count == 0)
            return Error.Validation(nameof(lineItems), "An order must have at least one line item.");

        if (lineItems.Select(li => li.ProductId).Distinct().Count() != lineItems.Count)
            return Error.Validation(nameof(lineItems),
                "The same product cannot appear in multiple line items. Combine quantities instead.");

        foreach (var li in lineItems)
        {
            var quantityCheck = ValidateQuantity(li.Quantity);
            if (quantityCheck.IsFailure)
                return quantityCheck.Error!;
        }

        var order = new Order(Guid.NewGuid(), customerId, createdByActorId, createdAt);
        foreach (var li in lineItems)
            order._lineItems.Add(new LineItem(li.ProductId, li.ProductName, li.Quantity, li.UnitPrice));

        return order;
    }

    public Result AddLineItem(Guid productId, string productName, int quantity, decimal unitPrice)
    {
        if (Status != OrderStatus.Draft)
            return Error.Validation(nameof(Status), "Line items can only be modified while the order is in Draft status.");

        var quantityCheck = ValidateQuantity(quantity);
        if (quantityCheck.IsFailure)
            return quantityCheck;

        if (_lineItems.Any(li => li.ProductId == productId))
            return Error.Validation(nameof(productId),
                "The product is already in the order. Remove it first or combine quantities.");

        _lineItems.Add(new LineItem(productId, productName, quantity, unitPrice));
        return Result.Success();
    }

    public Result RemoveLineItem(Guid lineItemId)
    {
        if (Status != OrderStatus.Draft)
            return Error.Validation(nameof(Status), "Line items can only be modified while the order is in Draft status.");

        var item = _lineItems.FirstOrDefault(li => li.Id == lineItemId);
        if (item is null)
            return Error.NotFound($"Line item '{lineItemId}' was not found in the order.");

        if (_lineItems.Count <= 1)
            return Error.Validation(nameof(LineItems), "Cannot remove the last line item from an order.");

        _lineItems.Remove(item);
        return Result.Success();
    }

    /// <summary>Draft → Submitted. Reserves stock for each line item from the supplied products.</summary>
    public Result Submit(IReadOnlyDictionary<Guid, Product> products, DateTimeOffset now)
    {
        if (Status != OrderStatus.Draft)
            return InvalidTransition(OrderStatus.Submitted);

        if (_lineItems.Count == 0)
            return Error.Validation(nameof(LineItems), "An order must have at least one line item before submission.");

        // Pre-check availability so no stock is mutated unless all line items can be fulfilled.
        foreach (var li in _lineItems)
        {
            if (!products.TryGetValue(li.ProductId, out var product))
                return Error.NotFound($"Product '{li.ProductId}' was not found.");

            if (li.Quantity > product.StockQuantity)
                return Error.Validation(nameof(LineItems),
                    $"Insufficient stock for product '{product.ProductName}'. Available: {product.StockQuantity}, requested: {li.Quantity}.");
        }

        foreach (var li in _lineItems)
        {
            var reserve = products[li.ProductId].ReserveStock(li.Quantity);
            if (reserve.IsFailure)
                return reserve;
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
            return Error.Validation(nameof(Status),
                $"An order in '{Status}' status cannot be cancelled.");

        var from = Status;

        if (from is OrderStatus.Submitted or OrderStatus.Approved)
        {
            foreach (var li in _lineItems)
            {
                if (products.TryGetValue(li.ProductId, out var product))
                    product.ReleaseStock(li.Quantity);
            }
        }

        Status = OrderStatus.Cancelled;
        _domainEvents.Add(new OrderCancelledEvent(Id, from, now));
        return Result.Success();
    }

    private static Result ValidateQuantity(int quantity)
    {
        if (quantity < LineItem.MinQuantity || quantity > LineItem.MaxQuantity)
            return Error.Validation("quantity",
                $"Quantity must be between {LineItem.MinQuantity} and {LineItem.MaxQuantity}.");
        return Result.Success();
    }

    private Result InvalidTransition(OrderStatus target)
        => Error.Validation(nameof(Status),
            $"Cannot transition order from '{Status}' to '{target}'.");
}
