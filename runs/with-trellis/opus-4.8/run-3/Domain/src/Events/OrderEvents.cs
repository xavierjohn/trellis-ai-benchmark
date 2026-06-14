namespace OrderManagement.Domain;

/// <summary>Raised when an order is submitted and stock reserved.</summary>
public sealed record OrderSubmittedEvent(
    OrderId OrderId,
    CustomerId CustomerId,
    decimal OrderTotal,
    DateTime SubmittedAt) : IDomainEvent
{
    /// <inheritdoc />
    public DateTimeOffset OccurredAt => SubmittedAt;
}

/// <summary>Raised when an order is approved.</summary>
public sealed record OrderApprovedEvent(OrderId OrderId, DateTime ApprovedAt) : IDomainEvent
{
    /// <inheritdoc />
    public DateTimeOffset OccurredAt => ApprovedAt;
}

/// <summary>Raised when an order is shipped.</summary>
public sealed record OrderShippedEvent(OrderId OrderId, CustomerId CustomerId, DateTime ShippedAt) : IDomainEvent
{
    /// <inheritdoc />
    public DateTimeOffset OccurredAt => ShippedAt;
}

/// <summary>Raised when an order is delivered.</summary>
public sealed record OrderDeliveredEvent(OrderId OrderId, DateTime DeliveredAt) : IDomainEvent
{
    /// <inheritdoc />
    public DateTimeOffset OccurredAt => DeliveredAt;
}

/// <summary>Raised when an order is cancelled.</summary>
public sealed record OrderCancelledEvent(
    OrderId OrderId,
    OrderStatus CancelledFromStatus,
    DateTime CancelledAt) : IDomainEvent
{
    /// <inheritdoc />
    public DateTimeOffset OccurredAt => CancelledAt;
}
