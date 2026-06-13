namespace OrderManagement.Domain.Events;

public sealed record OrderCancelledEvent(OrderId OrderId, string CancelledFromStatus, DateTimeOffset OccurredAt) : IDomainEvent;
