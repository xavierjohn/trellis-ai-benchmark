using System.Text.RegularExpressions;
using OrderManagement.Api.Domain.Common;

namespace OrderManagement.Api.Domain.Products;

/// <summary>Product aggregate root.</summary>
public sealed class Product
{
    private static readonly Regex SkuRegex = new(@"^[A-Z0-9]{3,20}$", RegexOptions.Compiled);

    private Product() { } // EF

    private Product(Guid id, string productName, string sku, decimal unitPrice, int stockQuantity)
    {
        Id = id;
        ProductName = productName;
        Sku = sku;
        UnitPrice = unitPrice;
        StockQuantity = stockQuantity;
    }

    public Guid Id { get; private set; }
    public string ProductName { get; private set; } = default!;
    public string Sku { get; private set; } = default!;
    public decimal UnitPrice { get; private set; }
    public int StockQuantity { get; private set; }

    public static Product Create(string productName, string sku, decimal unitPrice, int stockQuantity = 0)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(productName) || productName.Length > 200)
            errors[nameof(productName)] = ["ProductName is required and must be 1-200 characters."];

        var normalizedSku = sku?.Trim() ?? string.Empty;
        if (!SkuRegex.IsMatch(normalizedSku))
            errors[nameof(sku)] = ["SKU is required and must be 3-20 uppercase alphanumeric characters."];

        if (unitPrice <= 0m)
            errors[nameof(unitPrice)] = ["UnitPrice must be greater than zero."];

        if (stockQuantity < 0)
            errors[nameof(stockQuantity)] = ["StockQuantity cannot be negative."];

        if (errors.Count > 0)
            throw new ValidationAppException("Product validation failed.", errors);

        return new Product(Guid.NewGuid(), productName.Trim(), normalizedSku, unitPrice, stockQuantity);
    }

    /// <summary>Increases stock. Quantity must be positive.</summary>
    public void AddStock(int quantity)
    {
        if (quantity <= 0)
            throw new ValidationAppException("Add stock quantity must be positive.",
                new Dictionary<string, string[]> { ["quantity"] = ["Quantity must be a positive integer."] });

        StockQuantity += quantity;
    }

    /// <summary>Reserves (decreases) stock. Fails if insufficient.</summary>
    public void ReserveStock(int quantity)
    {
        if (quantity <= 0)
            throw new ValidationAppException("Reserve stock quantity must be positive.",
                new Dictionary<string, string[]> { ["quantity"] = ["Quantity must be a positive integer."] });

        if (quantity > StockQuantity)
            throw new ValidationAppException(
                $"Insufficient stock for product '{ProductName}' (SKU {Sku}): requested {quantity}, available {StockQuantity}.");

        StockQuantity -= quantity;
    }

    /// <summary>Releases (restores) previously reserved stock.</summary>
    public void ReleaseStock(int quantity)
    {
        if (quantity <= 0)
            throw new ValidationAppException("Release stock quantity must be positive.",
                new Dictionary<string, string[]> { ["quantity"] = ["Quantity must be a positive integer."] });

        StockQuantity += quantity;
    }
}
