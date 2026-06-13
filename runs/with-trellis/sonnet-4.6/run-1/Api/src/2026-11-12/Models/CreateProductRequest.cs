namespace OrderManagement.Api.v2026_11_12.Models;

using OrderManagement.Domain;

/// <summary>Create product request body.</summary>
public sealed record CreateProductRequest
{
    /// <summary>Product name.</summary>
    public ProductName Name { get; init; } = null!;
    /// <summary>SKU.</summary>
    public Sku Sku { get; init; } = null!;
    /// <summary>Unit price.</summary>
    public decimal UnitPrice { get; init; }
}
