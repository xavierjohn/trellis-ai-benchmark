using System.Text.RegularExpressions;
using OrderManagement.Domain.Common;

namespace OrderManagement.Domain.Products;

public sealed partial class Product
{
    [GeneratedRegex(@"^[A-Z0-9]{3,20}$")]
    private static partial Regex SkuRegex();

    private Product(Guid id, string productName, string sku, decimal unitPrice, int stockQuantity)
    {
        Id = id;
        ProductName = productName;
        Sku = sku;
        UnitPrice = unitPrice;
        StockQuantity = stockQuantity;
    }

    // Parameterless ctor for EF Core materialization.
    private Product() { }

    public Guid Id { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public int StockQuantity { get; private set; }

    public static Result<Product> Create(string? productName, string? sku, decimal unitPrice, int initialStock = 0)
    {
        var fieldErrors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(productName) || productName.Trim().Length > 200)
            fieldErrors["productName"] = ["productName is required and must be 1-200 characters."];

        if (string.IsNullOrWhiteSpace(sku) || !SkuRegex().IsMatch(sku.Trim()))
            fieldErrors["sku"] = ["sku is required, 3-20 characters, uppercase letters and digits only."];

        if (unitPrice <= 0m)
            fieldErrors["unitPrice"] = ["unitPrice must be greater than zero."];

        if (initialStock < 0)
            fieldErrors["stockQuantity"] = ["stockQuantity cannot be negative."];

        if (fieldErrors.Count > 0)
            return Error.Validation(fieldErrors);

        return new Product(Guid.NewGuid(), productName!.Trim(), sku!.Trim(), unitPrice, initialStock);
    }

    /// <summary>Increases stock. Quantity must be positive.</summary>
    public Result AddStock(int quantity)
    {
        if (quantity <= 0)
            return Error.Validation("quantity must be positive.");

        StockQuantity += quantity;
        return Result.Success();
    }

    /// <summary>Decreases stock. Fails if there is insufficient stock.</summary>
    public Result ReserveStock(int quantity)
    {
        if (quantity <= 0)
            return Error.Validation("quantity must be positive.");

        if (quantity > StockQuantity)
            return Error.Validation($"Insufficient stock for product '{ProductName}' (SKU {Sku}). Requested {quantity}, available {StockQuantity}.");

        StockQuantity -= quantity;
        return Result.Success();
    }

    /// <summary>Restores previously reserved stock.</summary>
    public void ReleaseStock(int quantity)
    {
        if (quantity <= 0)
            return;

        StockQuantity += quantity;
    }
}
