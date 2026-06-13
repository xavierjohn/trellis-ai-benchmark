namespace OrderManagement.Domain.Events;

public record OrderSubmittedEvent(
    Guid OrderId,
    Guid CustomerId,
    decimal OrderTotal,
    DateTimeOffset SubmittedAt);

public record OrderApprovedEvent(
    Guid OrderId,
    DateTimeOffset ApprovedAt);

public record OrderShippedEvent(
    Guid OrderId,
    Guid CustomerId,
    DateTimeOffset ShippedAt);

public record OrderDeliveredEvent(
    Guid OrderId,
    DateTimeOffset DeliveredAt);

public record OrderCancelledEvent(
    Guid OrderId,
    string CancelledFromStatus,
    DateTimeOffset CancelledAt);
