namespace OrderManagement.Api.v2026_11_12.Models;

using OrderManagement.Domain;

/// <summary>Response model for a product.</summary>
public record ProductResponse
{
    /// <summary>Unique identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Product name.</summary>
    public string ProductName { get; init; } = null!;

    /// <summary>SKU (stock keeping unit).</summary>
    public string Sku { get; init; } = null!;

    /// <summary>Unit price.</summary>
    public decimal UnitPrice { get; init; }

    /// <summary>Current stock quantity.</summary>
    public int StockQuantity { get; init; }

    /// <summary>When created.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>When last modified.</summary>
    public DateTimeOffset LastModified { get; init; }

    /// <summary>Maps from domain aggregate.</summary>
    public static ProductResponse From(Product product) => new()
    {
        Id = product.Id.Value,
        ProductName = product.ProductName.Value,
        Sku = product.Sku.Value,
        UnitPrice = product.UnitPrice.Value,
        StockQuantity = product.StockQuantity,
        CreatedAt = product.CreatedAt,
        LastModified = product.LastModified,
    };
}
