using OrderManagement.Domain.Common;

namespace OrderManagement.Domain.Products;

/// <summary>Product aggregate root. Tracks available stock.</summary>
public sealed class Product
{
    public Guid Id { get; private set; }
    public string ProductName { get; private set; } = null!;
    public Sku Sku { get; private set; } = null!;
    public decimal UnitPrice { get; private set; }
    public int StockQuantity { get; private set; }

    private Product() { } // EF

    private Product(Guid id, string productName, Sku sku, decimal unitPrice, int stockQuantity)
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

        if (string.IsNullOrWhiteSpace(productName) || productName.Trim().Length > 200)
            fields[nameof(ProductName)] = new[] { "ProductName is required and must be 1–200 characters." };

        var skuResult = Sku.Create(sku);
        if (skuResult.IsFailure)
            fields[nameof(Sku)] = new[] { skuResult.Error!.Message };

        if (unitPrice <= 0m)
            fields[nameof(UnitPrice)] = new[] { "UnitPrice must be greater than zero." };

        if (fields.Count > 0)
            return Error.Validation("Product is invalid.", fields);

        return new Product(Guid.NewGuid(), productName!.Trim(), skuResult.Value, unitPrice, 0);
    }

    /// <summary>Increases stock quantity. Quantity must be positive.</summary>
    public Result AddStock(int quantity)
    {
        if (quantity <= 0)
            return Error.Validation(nameof(quantity), "Quantity to add must be positive.");

        StockQuantity += quantity;
        return Result.Success();
    }

    /// <summary>Decreases stock quantity. Fails if insufficient stock.</summary>
    public Result ReserveStock(int quantity)
    {
        if (quantity <= 0)
            return Error.Validation(nameof(quantity), "Quantity to reserve must be positive.");

        if (quantity > StockQuantity)
            return Error.Validation(nameof(quantity),
                $"Insufficient stock for product '{ProductName}'. Available: {StockQuantity}, requested: {quantity}.");

        StockQuantity -= quantity;
        return Result.Success();
    }

    /// <summary>Restores previously reserved stock.</summary>
    public Result ReleaseStock(int quantity)
    {
        if (quantity <= 0)
            return Error.Validation(nameof(quantity), "Quantity to release must be positive.");

        StockQuantity += quantity;
        return Result.Success();
    }
}
