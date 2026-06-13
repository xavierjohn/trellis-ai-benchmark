using OrderManagement.Domain.Common;

namespace OrderManagement.Domain.Orders;

public sealed record OrderSubmittedEvent(Guid OrderId, Guid CustomerId, decimal OrderTotal, DateTimeOffset SubmittedAt)
    : IDomainEvent
{
    public DateTimeOffset OccurredAt => SubmittedAt;
}

public sealed record OrderApprovedEvent(Guid OrderId, DateTimeOffset ApprovedAt) : IDomainEvent
{
    public DateTimeOffset OccurredAt => ApprovedAt;
}

public sealed record OrderShippedEvent(Guid OrderId, Guid CustomerId, DateTimeOffset ShippedAt) : IDomainEvent
{
    public DateTimeOffset OccurredAt => ShippedAt;
}

public sealed record OrderDeliveredEvent(Guid OrderId, DateTimeOffset DeliveredAt) : IDomainEvent
{
    public DateTimeOffset OccurredAt => DeliveredAt;
}

public sealed record OrderCancelledEvent(Guid OrderId, OrderStatus CancelledFromStatus, DateTimeOffset CancelledAt)
    : IDomainEvent
{
    public DateTimeOffset OccurredAt => CancelledAt;
}
