namespace OrderManagement.Domain.Common;

/// <summary>Marker interface for domain events.</summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
