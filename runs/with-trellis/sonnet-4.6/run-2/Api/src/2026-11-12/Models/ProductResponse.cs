namespace OrderManagement.Api.v2026_11_12.Models;

using OrderManagement.Domain;

/// <summary>Response model for a product.</summary>
public record ProductResponse
{
    /// <summary>Product's unique identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Product name.</summary>
    public string ProductName { get; init; } = null!;

    /// <summary>Stock-keeping unit.</summary>
    public string SKU { get; init; } = null!;

    /// <summary>Unit price.</summary>
    public decimal UnitPrice { get; init; }

    /// <summary>Current stock quantity.</summary>
    public int StockQuantity { get; init; }

    /// <summary>When the product was created.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>Maps from domain aggregate to API response.</summary>
    public static ProductResponse From(Product product) => new()
    {
        Id = product.Id.Value,
        ProductName = product.ProductName.Value,
        SKU = product.SKU.Value,
        UnitPrice = product.UnitPrice.Value,
        StockQuantity = product.StockQuantity.Value,
        CreatedAt = product.CreatedAt
    };
}
