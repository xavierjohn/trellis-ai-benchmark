namespace OrderManagement.Domain;

using Trellis.Primitives;

/// <summary>
/// Raised when an order is submitted (Draft → Submitted).
/// </summary>
public sealed record OrderSubmittedEvent(OrderId OrderId, CustomerId CustomerId, MonetaryAmount OrderTotal, DateTimeOffset OccurredAt) : IDomainEvent;

/// <summary>
/// Raised when an order is approved (Submitted → Approved).
/// </summary>
public sealed record OrderApprovedEvent(OrderId OrderId, DateTimeOffset OccurredAt) : IDomainEvent;

/// <summary>
/// Raised when an order is shipped (Approved → Shipped).
/// </summary>
public sealed record OrderShippedEvent(OrderId OrderId, CustomerId CustomerId, DateTimeOffset OccurredAt) : IDomainEvent;

/// <summary>
/// Raised when an order is delivered (Shipped → Delivered).
/// </summary>
public sealed record OrderDeliveredEvent(OrderId OrderId, DateTimeOffset OccurredAt) : IDomainEvent;

/// <summary>
/// Raised when an order is cancelled.
/// </summary>
public sealed record OrderCancelledEvent(OrderId OrderId, OrderStatus CancelledFromStatus, DateTimeOffset OccurredAt) : IDomainEvent;
