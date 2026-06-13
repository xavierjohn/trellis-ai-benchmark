using OrderManagement.Domain.Enums;
using OrderManagement.Domain.Events;
using OrderManagement.Domain.Exceptions;

namespace OrderManagement.Domain.Aggregates;

public class Order
{
    private readonly List<LineItem> _lineItems = [];

    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public string CreatedByActorId { get; private set; } = default!;
    public OrderStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? SubmittedAt { get; private set; }
    public DateTimeOffset? ShippedAt { get; private set; }
    public IReadOnlyList<LineItem> LineItems => _lineItems.AsReadOnly();

    public decimal OrderTotal => _lineItems.Sum(li => li.LineTotal);

    private Order() { }

    public static Order Create(Guid customerId, string actorId, DateTimeOffset createdAt)
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            CreatedByActorId = actorId,
            Status = OrderStatus.Draft,
            CreatedAt = createdAt
        };
    }

    public void AddLineItem(Guid productId, string productName, int quantity, decimal unitPrice)
    {
        ValidateQuantity(quantity);

        if (_lineItems.Any(li => li.ProductId == productId))
            throw new ValidationException($"Product '{productName}' is already in this order. Combine quantities instead.");

        _lineItems.Add(LineItem.Create(Id, productId, productName, quantity, unitPrice));
    }

    public void RemoveLineItem(Guid lineItemId)
    {
        if (Status != OrderStatus.Draft)
            throw new ValidationException("Line items can only be removed from Draft orders.");

        var item = _lineItems.FirstOrDefault(li => li.Id == lineItemId)
            ?? throw new NotFoundException("LineItem", lineItemId);

        if (_lineItems.Count == 1)
            throw new ValidationException("Cannot remove the last line item from an order.");

        _lineItems.Remove(item);
    }

    public OrderSubmittedEvent Submit(DateTimeOffset now, Action<Guid, int> reserveStock)
    {
        if (Status != OrderStatus.Draft)
            throw new ValidationException($"Cannot submit an order in '{Status}' status. Only Draft orders can be submitted.");

        if (_lineItems.Count == 0)
            throw new ValidationException("Order must have at least one line item before submitting.");

        foreach (var item in _lineItems)
            reserveStock(item.ProductId, item.Quantity);

        Status = OrderStatus.Submitted;
        SubmittedAt = now;

        return new OrderSubmittedEvent(Id, CustomerId, OrderTotal, now);
    }

    public OrderApprovedEvent Approve(DateTimeOffset now)
    {
        if (Status != OrderStatus.Submitted)
            throw new ValidationException($"Cannot approve an order in '{Status}' status. Only Submitted orders can be approved.");

        Status = OrderStatus.Approved;
        return new OrderApprovedEvent(Id, now);
    }

    public OrderShippedEvent Ship(DateTimeOffset now)
    {
        if (Status != OrderStatus.Approved)
            throw new ValidationException($"Cannot ship an order in '{Status}' status. Only Approved orders can be shipped.");

        Status = OrderStatus.Shipped;
        ShippedAt = now;
        return new OrderShippedEvent(Id, CustomerId, now);
    }

    public OrderDeliveredEvent Deliver(DateTimeOffset now)
    {
        if (Status != OrderStatus.Shipped)
            throw new ValidationException($"Cannot deliver an order in '{Status}' status. Only Shipped orders can be delivered.");

        Status = OrderStatus.Delivered;
        return new OrderDeliveredEvent(Id, now);
    }

    public OrderCancelledEvent Cancel(DateTimeOffset now, Action<Guid, int> releaseStock)
    {
        if (Status is OrderStatus.Shipped or OrderStatus.Delivered)
            throw new ValidationException($"Cannot cancel an order in '{Status}' status.");

        var previousStatus = Status;

        if (Status is OrderStatus.Submitted or OrderStatus.Approved)
        {
            foreach (var item in _lineItems)
                releaseStock(item.ProductId, item.Quantity);
        }

        Status = OrderStatus.Cancelled;
        return new OrderCancelledEvent(Id, previousStatus.ToString(), now);
    }

    public bool IsOverdue(DateTimeOffset now)
    {
        return Status == OrderStatus.Submitted
            && SubmittedAt.HasValue
            && (now - SubmittedAt.Value).TotalDays > 7;
    }

    public void SetLineItems(IEnumerable<LineItem> items)
    {
        _lineItems.Clear();
        _lineItems.AddRange(items);
    }

    private static void ValidateQuantity(int quantity)
    {
        if (quantity < 1 || quantity > 999)
            throw new ValidationException("Quantity must be between 1 and 999.");
    }
}
