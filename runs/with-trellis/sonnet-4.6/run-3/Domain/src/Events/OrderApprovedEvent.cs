namespace OrderManagement.Domain.Events;

public sealed record OrderApprovedEvent(OrderId OrderId, DateTimeOffset OccurredAt) : IDomainEvent;
