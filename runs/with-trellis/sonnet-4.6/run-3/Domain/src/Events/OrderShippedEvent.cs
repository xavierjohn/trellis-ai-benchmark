namespace OrderManagement.Domain.Events;

public sealed record OrderShippedEvent(OrderId OrderId, CustomerId CustomerId, DateTimeOffset OccurredAt) : IDomainEvent;
