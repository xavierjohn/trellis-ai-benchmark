namespace OrderManagement.Api.v2026_11_12.Models;

using OrderManagement.Domain;

/// <summary>Order line item request body.</summary>
public sealed record CreateOrderLineItemRequest
{
    /// <summary>Product identifier.</summary>
    public ProductId ProductId { get; init; } = null!;
    /// <summary>Quantity.</summary>
    public Quantity Quantity { get; init; } = null!;
}
