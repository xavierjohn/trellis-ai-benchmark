namespace OrderManagement.Domain;

/// <summary>Raised when an order is submitted.</summary>
public sealed record OrderSubmittedEvent(OrderId OrderId, CustomerId CustomerId, decimal OrderTotal, DateTime SubmittedAt, DateTimeOffset OccurredAt) : IDomainEvent;

/// <summary>Raised when an order is approved.</summary>
public sealed record OrderApprovedEvent(OrderId OrderId, DateTime ApprovedAt, DateTimeOffset OccurredAt) : IDomainEvent;

/// <summary>Raised when an order is shipped.</summary>
public sealed record OrderShippedEvent(OrderId OrderId, CustomerId CustomerId, DateTime ShippedAt, DateTimeOffset OccurredAt) : IDomainEvent;

/// <summary>Raised when an order is delivered.</summary>
public sealed record OrderDeliveredEvent(OrderId OrderId, DateTime DeliveredAt, DateTimeOffset OccurredAt) : IDomainEvent;

/// <summary>Raised when an order is cancelled.</summary>
public sealed record OrderCancelledEvent(OrderId OrderId, OrderStatus CancelledFromStatus, DateTime CancelledAt, DateTimeOffset OccurredAt) : IDomainEvent;
