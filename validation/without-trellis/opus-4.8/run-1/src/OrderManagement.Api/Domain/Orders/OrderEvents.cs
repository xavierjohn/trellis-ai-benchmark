using OrderManagement.Api.Domain.Common;

namespace OrderManagement.Api.Domain.Orders;

public sealed record OrderSubmittedEvent(Guid OrderId, Guid CustomerId, decimal OrderTotal, DateTime SubmittedAt) : DomainEvent;

public sealed record OrderApprovedEvent(Guid OrderId, DateTime ApprovedAt) : DomainEvent;

public sealed record OrderShippedEvent(Guid OrderId, Guid CustomerId, DateTime ShippedAt) : DomainEvent;

public sealed record OrderDeliveredEvent(Guid OrderId, DateTime DeliveredAt) : DomainEvent;

public sealed record OrderCancelledEvent(Guid OrderId, OrderStatus CancelledFromStatus, DateTime CancelledAt) : DomainEvent;
