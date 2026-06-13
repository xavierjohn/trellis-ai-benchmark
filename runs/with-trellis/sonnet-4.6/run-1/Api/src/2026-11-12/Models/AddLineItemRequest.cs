namespace OrderManagement.Api.v2026_11_12.Models;

using OrderManagement.Domain;

/// <summary>Add line item request body.</summary>
public sealed record AddLineItemRequest
{
    /// <summary>Product identifier.</summary>
    public ProductId ProductId { get; init; } = null!;
    /// <summary>Quantity.</summary>
    public Quantity Quantity { get; init; } = null!;
}
