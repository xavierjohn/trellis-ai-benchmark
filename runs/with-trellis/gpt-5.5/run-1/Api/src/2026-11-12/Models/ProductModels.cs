namespace OrderManagement.Api.v2026_11_12.Models;

using OrderManagement.Domain;
using Trellis.Primitives;

/// <summary>
/// Create product request.
/// </summary>
public sealed record CreateProductRequest
{
    /// <summary>Product name.</summary>
    public ProductName ProductName { get; init; } = null!;

    /// <summary>Unique SKU.</summary>
    public Sku Sku { get; init; } = null!;

    /// <summary>Unit price in USD.</summary>
    public MonetaryAmount UnitPrice { get; init; } = null!;
}

/// <summary>
/// Add stock request.
/// </summary>
public sealed record AddStockRequest
{
    /// <summary>Quantity to add.</summary>
    public StockAdjustmentQuantity Quantity { get; init; } = null!;
}

/// <summary>
/// Product response.
/// </summary>
public sealed record ProductResponse
{
    /// <summary>Product ID.</summary>
    public Guid Id { get; init; }

    /// <summary>Product name.</summary>
    public string ProductName { get; init; } = null!;

    /// <summary>SKU.</summary>
    public string Sku { get; init; } = null!;

    /// <summary>Unit price.</summary>
    public decimal UnitPrice { get; init; }

    /// <summary>Available stock.</summary>
    public int StockQuantity { get; init; }

    /// <summary>Maps a domain product.</summary>
    public static ProductResponse From(Product product) => new()
    {
        Id = product.Id.Value,
        ProductName = product.Name.Value,
        Sku = product.Sku.Value,
        UnitPrice = product.UnitPrice.Value,
        StockQuantity = product.StockQuantity.Value,
    };
}
