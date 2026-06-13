namespace OrderManagement.Api.v2026_11_12.Models;

using OrderManagement.Domain;

/// <summary>Request body for creating a product.</summary>
/// <param name="Name">Product name.</param>
/// <param name="Sku">Stock Keeping Unit (unique across products).</param>
/// <param name="UnitPrice">Unit price in USD (must be greater than zero).</param>
public sealed record CreateProductRequest(ProductName Name, Sku Sku, UnitPrice UnitPrice);

/// <summary>Request body for adding stock to a product.</summary>
/// <param name="Quantity">Quantity to add (must be positive).</param>
public sealed record AddStockRequest(int Quantity);

/// <summary>Response body representing a product.</summary>
/// <param name="Id">Product identifier.</param>
/// <param name="Name">Product name.</param>
/// <param name="Sku">Stock Keeping Unit.</param>
/// <param name="UnitPrice">Unit price in USD.</param>
/// <param name="StockQuantity">Current available stock quantity.</param>
public sealed record ProductResponse(Guid Id, string Name, string Sku, decimal UnitPrice, int StockQuantity)
{
    /// <summary>Projects a domain <see cref="Product"/> to a response.</summary>
    public static ProductResponse From(Product product) =>
        new(product.Id.Value, product.Name.Value, product.Sku.Value, product.UnitPrice.Value, product.StockQuantity);
}
