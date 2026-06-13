namespace OrderManagement.Api.Domain;

public class Order
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public string CreatedByActorId { get; private set; } = "";
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? SubmittedAt { get; private set; }
    public DateTime? ShippedAt { get; private set; }

    public List<LineItem> LineItems { get; private set; } = new();

    public decimal OrderTotal => LineItems.Sum(li => li.UnitPrice * li.Quantity);

    // Determines whether stock needs to be released on cancel
    public bool RequiresStockRelease =>
        Status == OrderStatus.Submitted || Status == OrderStatus.Approved;

    private Order() { } // EF Core

    public static Order Create(Guid customerId, string createdByActorId, DateTime createdAt)
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            CreatedByActorId = createdByActorId,
            Status = OrderStatus.Draft,
            CreatedAt = createdAt,
            LineItems = new List<LineItem>()
        };
    }

    public void AddLineItem(LineItem lineItem)
    {
        if (Status != OrderStatus.Draft)
            throw new DomainValidationException(
                "Can only add line items to a Draft order.");

        if (LineItems.Any(li => li.ProductId == lineItem.ProductId))
            throw new DomainValidationException(
                $"Product '{lineItem.ProductName}' is already in this order. Combine quantities instead.");

        LineItems.Add(lineItem);
    }

    public void RemoveLineItem(Guid lineItemId)
    {
        if (Status != OrderStatus.Draft)
            throw new DomainValidationException(
                "Can only remove line items from a Draft order.");

        var lineItem = LineItems.FirstOrDefault(li => li.Id == lineItemId)
            ?? throw new NotFoundException($"Line item {lineItemId} not found in order.");

        if (LineItems.Count == 1)
            throw new DomainValidationException(
                "Cannot remove the last line item from an order.");

        LineItems.Remove(lineItem);
    }

    public void Submit(TimeProvider timeProvider)
    {
        if (Status != OrderStatus.Draft)
            throw new DomainValidationException(
                $"Cannot submit an order in '{Status}' status. Order must be in Draft status.");

        if (LineItems.Count == 0)
            throw new DomainValidationException(
                "Order must have at least one line item to be submitted.");

        Status = OrderStatus.Submitted;
        SubmittedAt = timeProvider.GetUtcNow().UtcDateTime;
    }

    public void Approve(TimeProvider timeProvider)
    {
        if (Status != OrderStatus.Submitted)
            throw new DomainValidationException(
                $"Cannot approve an order in '{Status}' status. Order must be in Submitted status.");

        Status = OrderStatus.Approved;
    }

    public void Ship(TimeProvider timeProvider)
    {
        if (Status != OrderStatus.Approved)
            throw new DomainValidationException(
                $"Cannot ship an order in '{Status}' status. Order must be in Approved status.");

        Status = OrderStatus.Shipped;
        ShippedAt = timeProvider.GetUtcNow().UtcDateTime;
    }

    public void Deliver(TimeProvider timeProvider)
    {
        if (Status != OrderStatus.Shipped)
            throw new DomainValidationException(
                $"Cannot deliver an order in '{Status}' status. Order must be in Shipped status.");

        Status = OrderStatus.Delivered;
    }

    public void Cancel(TimeProvider timeProvider)
    {
        if (Status == OrderStatus.Shipped || Status == OrderStatus.Delivered)
            throw new DomainValidationException(
                $"Cannot cancel an order in '{Status}' status.");

        if (Status == OrderStatus.Cancelled)
            throw new DomainValidationException("Order is already cancelled.");

        Status = OrderStatus.Cancelled;
    }
}
