namespace OrderManagement.Api.v2026_11_12.Models;

using OrderManagement.Domain;

/// <summary>Response body for a product.</summary>
/// <param name="Id">Product id.</param>
/// <param name="Name">Product name.</param>
/// <param name="Sku">Stock keeping unit.</param>
/// <param name="UnitPrice">Unit price (USD).</param>
/// <param name="StockQuantity">Current stock quantity.</param>
public sealed record ProductResponse(Guid Id, string Name, string Sku, decimal UnitPrice, int StockQuantity)
{
    /// <summary>Projects a domain product to its response representation.</summary>
    public static ProductResponse From(Product product) =>
        new(product.Id.Value, product.Name.Value, product.Sku.Value, product.UnitPrice.Value, product.StockQuantity);
}
