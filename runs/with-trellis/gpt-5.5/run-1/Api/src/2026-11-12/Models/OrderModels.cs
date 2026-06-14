namespace OrderManagement.Api.v2026_11_12.Models;

using OrderManagement.Domain;

/// <summary>
/// Create order line-item request.
/// </summary>
public sealed record DraftOrderLineRequest
{
    /// <summary>Product ID.</summary>
    public ProductId ProductId { get; init; } = null!;

    /// <summary>Quantity.</summary>
    public LineItemQuantity Quantity { get; init; } = null!;
}

/// <summary>
/// Create draft order request.
/// </summary>
public sealed record CreateDraftOrderRequest
{
    /// <summary>Customer ID.</summary>
    public CustomerId CustomerId { get; init; } = null!;

    /// <summary>Line items.</summary>
    public IReadOnlyList<DraftOrderLineRequest> LineItems { get; init; } = [];
}

/// <summary>
/// Add line item request.
/// </summary>
public sealed record AddLineItemRequest
{
    /// <summary>Product ID.</summary>
    public ProductId ProductId { get; init; } = null!;

    /// <summary>Quantity.</summary>
    public LineItemQuantity Quantity { get; init; } = null!;
}

/// <summary>
/// Money response.
/// </summary>
public sealed record MoneyResponse
{
    /// <summary>Amount.</summary>
    public decimal Amount { get; init; }

    /// <summary>Currency code.</summary>
    public string Currency { get; init; } = null!;

    /// <summary>Maps domain money.</summary>
    public static MoneyResponse From(Trellis.Primitives.Money money) => new()
    {
        Amount = money.Amount,
        Currency = money.Currency.Value,
    };
}

/// <summary>
/// Order line item response.
/// </summary>
public sealed record OrderLineItemResponse
{
    /// <summary>Line item ID.</summary>
    public Guid Id { get; init; }

    /// <summary>Product ID.</summary>
    public Guid ProductId { get; init; }

    /// <summary>Product name snapshot.</summary>
    public string ProductName { get; init; } = null!;

    /// <summary>Quantity.</summary>
    public int Quantity { get; init; }

    /// <summary>Unit price snapshot.</summary>
    public decimal UnitPrice { get; init; }

    /// <summary>Line total.</summary>
    public MoneyResponse LineTotal { get; init; } = null!;

    /// <summary>Maps a domain line item.</summary>
    public static OrderLineItemResponse From(OrderLineItem line) => new()
    {
        Id = line.Id.Value,
        ProductId = line.ProductId.Value,
        ProductName = line.ProductName.Value,
        Quantity = line.Quantity.Value,
        UnitPrice = line.UnitPrice.Value,
        LineTotal = MoneyResponse.From(line.GetTotal()),
    };
}

/// <summary>
/// Order response.
/// </summary>
public sealed record OrderResponse
{
    /// <summary>Order ID.</summary>
    public Guid Id { get; init; }

    /// <summary>Customer ID.</summary>
    public Guid CustomerId { get; init; }

    /// <summary>Actor that created the order.</summary>
    public string CreatedByActorId { get; init; } = null!;

    /// <summary>Order status.</summary>
    public string Status { get; init; } = null!;

    /// <summary>Line items.</summary>
    public IReadOnlyList<OrderLineItemResponse> LineItems { get; init; } = [];

    /// <summary>Order total.</summary>
    public MoneyResponse Total { get; init; } = null!;

    /// <summary>Submitted timestamp.</summary>
    public DateTime? SubmittedAt { get; init; }

    /// <summary>Shipped timestamp.</summary>
    public DateTime? ShippedAt { get; init; }

    /// <summary>Created timestamp.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>Last modified timestamp.</summary>
    public DateTimeOffset LastModified { get; init; }

    /// <summary>Maps a domain order.</summary>
    public static OrderResponse From(Order order) => new()
    {
        Id = order.Id.Value,
        CustomerId = order.CustomerId.Value,
        CreatedByActorId = order.CreatedByActorId,
        Status = order.Status.Value,
        LineItems = order.LineItems.Select(OrderLineItemResponse.From).ToArray(),
        Total = MoneyResponse.From(order.GetTotal()),
        SubmittedAt = order.SubmittedAt.AsNullable(),
        ShippedAt = order.ShippedAt.AsNullable(),
        CreatedAt = order.CreatedAt,
        LastModified = order.LastModified,
    };
}
