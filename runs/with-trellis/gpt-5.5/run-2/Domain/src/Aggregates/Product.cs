namespace OrderManagement.Domain;

/// <summary>Product aggregate.</summary>
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

    /// <summary>Create a product.</summary>
    public Product(ProductName productName, Sku sku, UnitPrice unitPrice)
        : base(ProductId.NewUniqueV7())
    {
        ProductName = productName;
        Sku = sku;
        UnitPrice = unitPrice;
        StockQuantity = StockQuantity.Create(0);
    }

    /// <summary>Add available stock.</summary>
    public Result<Product> AddStock(OrderQuantity quantity)
    {
        StockQuantity = StockQuantity.Create(StockQuantity.Value + quantity.Value);
        return Result.Ok(this);
    }

    /// <summary>Reserve stock for an order.</summary>
    public Result<Product> ReserveStock(OrderQuantity quantity)
    {
        if (StockQuantity.Value < quantity.Value)
        {
            return Result.Fail<Product>(Error.InvalidInput.ForRule(
                "product.insufficient_stock",
                $"Product {Sku.Value} has insufficient stock."));
        }

        ApplyReservedStock(quantity);
        return Result.Ok(this);
    }

    /// <summary>Release previously reserved stock.</summary>
    public Result<Product> ReleaseStock(OrderQuantity quantity)
    {
        ApplyReleasedStock(quantity);
        return Result.Ok(this);
    }

    internal void ApplyReservedStock(OrderQuantity quantity) =>
        StockQuantity = StockQuantity.Create(StockQuantity.Value - quantity.Value);

    internal void ApplyReleasedStock(OrderQuantity quantity) =>
        StockQuantity = StockQuantity.Create(StockQuantity.Value + quantity.Value);
}
