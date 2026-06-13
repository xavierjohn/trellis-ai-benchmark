namespace OrderManagement.Api.v2026_11_12.Models;

using OrderManagement.Domain;

/// <summary>Response model for an order.</summary>
public record OrderResponse
{
    /// <summary>Order's unique identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Customer who placed the order.</summary>
    public Guid CustomerId { get; init; }

    /// <summary>Actor who created the order.</summary>
    public string CreatedByActorId { get; init; } = null!;

    /// <summary>Current order status.</summary>
    public string Status { get; init; } = null!;

    /// <summary>Total order value.</summary>
    public decimal OrderTotal { get; init; }

    /// <summary>When the order was created.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>When the order was submitted, if applicable.</summary>
    public DateTime? SubmittedAt { get; init; }

    /// <summary>When the order was shipped, if applicable.</summary>
    public DateTime? ShippedAt { get; init; }

    /// <summary>Line items in the order.</summary>
    public IReadOnlyList<LineItemResponse> LineItems { get; init; } = [];

    /// <summary>Maps from domain aggregate to API response.</summary>
    public static OrderResponse From(Order order) => new()
    {
        Id = order.Id.Value,
        CustomerId = order.CustomerId.Value,
        CreatedByActorId = order.CreatedByActorId,
        Status = order.Status.Value,
        OrderTotal = order.OrderTotal,
        CreatedAt = order.CreatedAt,
        SubmittedAt = order.SubmittedAt.AsNullable(),
        ShippedAt = order.ShippedAt.AsNullable(),
        LineItems = order.LineItems.Select(LineItemResponse.From).ToList()
    };
}

/// <summary>Response model for an order line item.</summary>
public record LineItemResponse
{
    /// <summary>Line item unique identifier.</summary>
    public Guid LineItemId { get; init; }

    /// <summary>Product identifier.</summary>
    public Guid ProductId { get; init; }

    /// <summary>Product name at time of order.</summary>
    public string ProductName { get; init; } = null!;

    /// <summary>Quantity ordered.</summary>
    public int Quantity { get; init; }

    /// <summary>Unit price at time of order.</summary>
    public decimal UnitPrice { get; init; }

    /// <summary>Total for this line item.</summary>
    public decimal LineTotal { get; init; }

    /// <summary>Maps from domain entity to API response.</summary>
    public static LineItemResponse From(LineItem lineItem) => new()
    {
        LineItemId = lineItem.Id.Value,
        ProductId = lineItem.ProductId.Value,
        ProductName = lineItem.ProductName.Value,
        Quantity = lineItem.Quantity.Value,
        UnitPrice = lineItem.UnitPrice.Value,
        LineTotal = lineItem.UnitPrice.Value * lineItem.Quantity.Value
    };
}
