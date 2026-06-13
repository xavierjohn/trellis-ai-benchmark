namespace OrderManagement.Api.v2026_11_12.Models;

using OrderManagement.Domain;

/// <summary>Product response model.</summary>
public sealed record ProductResponse
{
    /// <summary>Product identifier.</summary>
    public Guid Id { get; init; }
    /// <summary>Product name.</summary>
    public string Name { get; init; } = null!;
    /// <summary>SKU.</summary>
    public string Sku { get; init; } = null!;
    /// <summary>Unit price.</summary>
    public decimal UnitPrice { get; init; }
    /// <summary>Available stock quantity.</summary>
    public int StockQuantity { get; init; }
    /// <summary>Created timestamp.</summary>
    public DateTimeOffset CreatedAt { get; init; }
    /// <summary>Last modified timestamp.</summary>
    public DateTimeOffset LastModified { get; init; }

    /// <summary>Maps from the domain aggregate.</summary>
    public static ProductResponse From(Product product) => new()
    {
        Id = product.Id.Value,
        Name = product.Name.Value,
        Sku = product.Sku.Value,
        UnitPrice = product.UnitPrice,
        StockQuantity = product.StockQuantity,
        CreatedAt = product.CreatedAt,
        LastModified = product.LastModified,
    };
}
