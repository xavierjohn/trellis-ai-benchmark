using System.Text.RegularExpressions;
using OrderManagement.Domain.Common;

namespace OrderManagement.Domain.Products;

public sealed class Product
{
    private static readonly Regex SkuRegex = new("^[A-Z0-9]{3,20}$", RegexOptions.Compiled);

    public Guid Id { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public int StockQuantity { get; private set; }

    private Product() { } // EF

    private Product(Guid id, string productName, string sku, decimal unitPrice, int stockQuantity)
    {
        Id = id;
        ProductName = productName;
        Sku = sku;
        UnitPrice = unitPrice;
        StockQuantity = stockQuantity;
    }

    public static Result<Product> Create(string? productName, string? sku, decimal unitPrice)
    {
        var fields = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(productName) || productName.Trim().Length is < 1 or > 200)
            fields["productName"] = ["Product name is required and must be 1-200 characters."];

        var normalizedSku = sku?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedSku) || !SkuRegex.IsMatch(normalizedSku))
            fields["sku"] = ["SKU is required and must be 3-20 uppercase alphanumeric characters."];

        if (unitPrice <= 0m)
            fields["unitPrice"] = ["Unit price must be greater than zero."];

        if (fields.Count > 0)
            return Error.Validation(fields);

        return new Product(Guid.NewGuid(), productName!.Trim(), normalizedSku, unitPrice, 0);
    }

    public Result AddStock(int quantity)
    {
        if (quantity <= 0)
            return Error.Validation("Quantity to add must be positive.");

        StockQuantity += quantity;
        return Result.Success();
    }

    public Result ReserveStock(int quantity)
    {
        if (quantity <= 0)
            return Error.Validation("Quantity to reserve must be positive.");

        if (quantity > StockQuantity)
            return Error.Validation(
                $"Insufficient stock for product '{ProductName}' (SKU {Sku}): requested {quantity}, available {StockQuantity}.");

        StockQuantity -= quantity;
        return Result.Success();
    }

    public void ReleaseStock(int quantity)
    {
        if (quantity <= 0)
            return;

        StockQuantity += quantity;
    }
}
