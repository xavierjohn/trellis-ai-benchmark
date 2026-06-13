namespace OrderManagement.Api.v2026_11_12.Models;

using OrderManagement.Domain;

/// <summary>Order response model.</summary>
public sealed record OrderResponse
{
    /// <summary>Order identifier.</summary>
    public Guid Id { get; init; }
    /// <summary>Customer identifier.</summary>
    public Guid CustomerId { get; init; }
    /// <summary>Actor that created the order.</summary>
    public string CreatedByActorId { get; init; } = null!;
    /// <summary>Current status.</summary>
    public string Status { get; init; } = null!;
    /// <summary>Order total.</summary>
    public decimal OrderTotal { get; init; }
    /// <summary>Line items.</summary>
    public IReadOnlyList<LineItemResponse> LineItems { get; init; } = [];
    /// <summary>Submitted timestamp when present.</summary>
    public DateTime? SubmittedAt { get; init; }
    /// <summary>Shipped timestamp when present.</summary>
    public DateTime? ShippedAt { get; init; }
    /// <summary>Created timestamp.</summary>
    public DateTimeOffset CreatedAt { get; init; }
    /// <summary>Last modified timestamp.</summary>
    public DateTimeOffset LastModified { get; init; }

    /// <summary>Maps from the domain aggregate.</summary>
    public static OrderResponse From(Order order) => new()
    {
        Id = order.Id.Value,
        CustomerId = order.CustomerId.Value,
        CreatedByActorId = order.CreatedByActorId.Value,
        Status = order.Status.Value,
        OrderTotal = order.OrderTotal,
        LineItems = order.LineItems.Select(LineItemResponse.From).ToList(),
        SubmittedAt = order.SubmittedAt.TryGetValue(out var submittedAt) ? submittedAt : null,
        ShippedAt = order.ShippedAt.TryGetValue(out var shippedAt) ? shippedAt : null,
        CreatedAt = order.CreatedAt,
        LastModified = order.LastModified,
    };
}
