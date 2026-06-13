namespace OrderManagement.Domain.Events;

public sealed record OrderSubmittedEvent(OrderId OrderId, CustomerId CustomerId, decimal OrderTotal, DateTimeOffset OccurredAt) : IDomainEvent;
