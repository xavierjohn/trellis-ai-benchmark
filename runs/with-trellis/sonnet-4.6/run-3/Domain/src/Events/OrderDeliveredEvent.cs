namespace OrderManagement.Domain.Events;

public sealed record OrderDeliveredEvent(OrderId OrderId, DateTimeOffset OccurredAt) : IDomainEvent;
