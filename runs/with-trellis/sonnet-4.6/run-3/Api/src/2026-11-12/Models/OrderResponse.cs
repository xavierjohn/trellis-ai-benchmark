namespace OrderManagement.Api.v2026_11_12.Models;

using OrderManagement.Domain;

/// <summary>Response model for an order.</summary>
public record OrderResponse
{
    /// <summary>Unique identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Customer who placed the order.</summary>
    public Guid CustomerId { get; init; }

    /// <summary>Actor who created the order.</summary>
    public string CreatedByActorId { get; init; } = null!;

    /// <summary>Current status.</summary>
    public string Status { get; init; } = null!;

    /// <summary>Order line items.</summary>
    public IReadOnlyList<LineItemResponse> LineItems { get; init; } = [];

    /// <summary>Order total.</summary>
    public decimal OrderTotal { get; init; }

    /// <summary>When submitted.</summary>
    public DateTime? SubmittedAt { get; init; }

    /// <summary>When shipped.</summary>
    public DateTime? ShippedAt { get; init; }

    /// <summary>When created.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>When last modified.</summary>
    public DateTimeOffset LastModified { get; init; }

    /// <summary>Maps from domain aggregate.</summary>
    public static OrderResponse From(Order order) => new()
    {
        Id = order.Id.Value,
        CustomerId = order.CustomerId.Value,
        CreatedByActorId = order.CreatedByActorId,
        Status = order.Status.Value,
        LineItems = order.LineItems.Select(LineItemResponse.From).ToList(),
        OrderTotal = order.OrderTotal,
        SubmittedAt = order.SubmittedAt.AsNullable(),
        ShippedAt = order.ShippedAt.AsNullable(),
        CreatedAt = order.CreatedAt,
        LastModified = order.LastModified,
    };
}

/// <summary>Response model for a line item.</summary>
public record LineItemResponse
{
    /// <summary>Unique identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Product ID.</summary>
    public Guid ProductId { get; init; }

    /// <summary>Product name at time of ordering.</summary>
    public string ProductName { get; init; } = null!;

    /// <summary>Quantity.</summary>
    public int Quantity { get; init; }

    /// <summary>Unit price at time of ordering.</summary>
    public decimal UnitPrice { get; init; }

    /// <summary>Line total.</summary>
    public decimal Total { get; init; }

    /// <summary>Maps from domain entity.</summary>
    public static LineItemResponse From(LineItem item) => new()
    {
        Id = item.Id.Value,
        ProductId = item.ProductId.Value,
        ProductName = item.ProductName.Value,
        Quantity = item.Quantity,
        UnitPrice = item.UnitPrice.Value,
        Total = item.Total,
    };
}
