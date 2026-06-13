namespace OrderManagement.Domain.Common;

/// <summary>Marker interface for domain events raised by aggregates.</summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
