namespace OrderManagement.Domain;

/// <summary>
/// Product aggregate.
/// </summary>
public class Product : Aggregate<ProductId>
{
    /// <summary>Product name.</summary>
    public ProductName ProductName { get; private set; } = null!;

    /// <summary>Unique SKU.</summary>
    public Sku Sku { get; private set; } = null!;

    /// <summary>Unit price in USD.</summary>
    public UnitPrice UnitPrice { get; private set; } = null!;

    /// <summary>Available stock quantity.</summary>
    public StockQuantity StockQuantity { get; private set; } = null!;

    private Product() : base(default!)
    {
    }

    /// <summary>Create a product from validated value objects.</summary>
    public Product(ProductName productName, Sku sku, UnitPrice unitPrice, TimeProvider timeProvider)
        : base(ProductId.NewUniqueV7(timeProvider))
    {
        ProductName = productName;
        Sku = sku;
        UnitPrice = unitPrice;
        StockQuantity = StockQuantity.Create(0);
    }

    /// <summary>Add available stock.</summary>
    public Result<Product> AddStock(StockAdjustmentQuantity quantity)
    {
        StockQuantity = StockQuantity.Create(StockQuantity.Value + quantity.Value);
        return Result.Ok(this);
    }

    /// <summary>Reserve available stock.</summary>
    public Result<Product> ReserveStock(StockAdjustmentQuantity quantity)
    {
        if (StockQuantity.Value < quantity.Value)
        {
            return Result.Fail<Product>(
                Error.InvalidInput.ForRule(
                    "products.insufficient_stock",
                    $"Insufficient stock for SKU {Sku.Value}."));
        }

        StockQuantity = StockQuantity.Create(StockQuantity.Value - quantity.Value);
        return Result.Ok(this);
    }
}
