namespace OrderManagement.Domain;

/// <summary>Raised when an order transitions from Draft to Submitted.</summary>
public sealed record OrderSubmittedEvent(OrderId OrderId, CustomerId CustomerId, decimal OrderTotal, DateTime SubmittedAt) : IDomainEvent
{
    /// <inheritdoc />
    public DateTimeOffset OccurredAt => new(SubmittedAt, TimeSpan.Zero);
}

/// <summary>Raised when an order transitions from Submitted to Approved.</summary>
public sealed record OrderApprovedEvent(OrderId OrderId, DateTime ApprovedAt) : IDomainEvent
{
    /// <inheritdoc />
    public DateTimeOffset OccurredAt => new(ApprovedAt, TimeSpan.Zero);
}

/// <summary>Raised when an order transitions from Approved to Shipped.</summary>
public sealed record OrderShippedEvent(OrderId OrderId, CustomerId CustomerId, DateTime ShippedAt) : IDomainEvent
{
    /// <inheritdoc />
    public DateTimeOffset OccurredAt => new(ShippedAt, TimeSpan.Zero);
}

/// <summary>Raised when an order transitions from Shipped to Delivered.</summary>
public sealed record OrderDeliveredEvent(OrderId OrderId, DateTime DeliveredAt) : IDomainEvent
{
    /// <inheritdoc />
    public DateTimeOffset OccurredAt => new(DeliveredAt, TimeSpan.Zero);
}

/// <summary>Raised when an order is cancelled.</summary>
public sealed record OrderCancelledEvent(OrderId OrderId, OrderStatus CancelledFromStatus, DateTime CancelledAt) : IDomainEvent
{
    /// <inheritdoc />
    public DateTimeOffset OccurredAt => new(CancelledAt, TimeSpan.Zero);
}
