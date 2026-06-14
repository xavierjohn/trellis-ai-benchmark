namespace OrderManagement.Api.Domain.Common;

/// <summary>Marker base type for domain events raised by aggregates.</summary>
public abstract record DomainEvent
{
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
