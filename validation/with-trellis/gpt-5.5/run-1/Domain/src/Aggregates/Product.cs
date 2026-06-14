namespace OrderManagement.Domain;

/// <summary>Product aggregate.</summary>
public class Product : Aggregate<ProductId>
{
    private Product() : base(default!)
    {
        Name = null!;
        Sku = null!;
        UnitPrice = null!;
        StockQuantity = null!;
    }

    private Product(ProductName name, Sku sku, UnitPrice unitPrice)
        : base(ProductId.NewUniqueV7())
    {
        Name = name;
        Sku = sku;
        UnitPrice = unitPrice;
        StockQuantity = StockQuantity.Create(0);
    }

    /// <summary>Product name.</summary>
    public ProductName Name { get; private set; }

    /// <summary>Unique SKU.</summary>
    public Sku Sku { get; private set; }

    /// <summary>Unit price in USD.</summary>
    public UnitPrice UnitPrice { get; private set; }

    /// <summary>Available stock quantity.</summary>
    public StockQuantity StockQuantity { get; private set; }

    /// <summary>Creates a product.</summary>
    public static Result<Product> TryCreate(ProductName name, Sku sku, UnitPrice unitPrice) =>
        Result.Ok(new Product(name, sku, unitPrice));

    /// <summary>Adds stock.</summary>
    public Result<Product> AddStock(StockAdjustmentQuantity quantity)
    {
        StockQuantity = StockQuantity.Create(StockQuantity.Value + quantity.Value);
        return Result.Ok(this);
    }

    /// <summary>Returns success if enough stock exists.</summary>
    public Result<Product> EnsureHasStock(OrderQuantity quantity) =>
        StockQuantity.Value >= quantity.Value
            ? Result.Ok(this)
            : Result.Fail<Product>(Error.InvalidInput.ForRule("product.insufficient_stock", $"Insufficient stock for SKU {Sku.Value}."));

    /// <summary>Reserves stock.</summary>
    public Result<Product> ReserveStock(OrderQuantity quantity) =>
        EnsureHasStock(quantity)
            .Tap(_ => StockQuantity = StockQuantity.Create(StockQuantity.Value - quantity.Value));

    /// <summary>Releases reserved stock.</summary>
    public Result<Product> ReleaseStock(OrderQuantity quantity)
    {
        StockQuantity = StockQuantity.Create(StockQuantity.Value + quantity.Value);
        return Result.Ok(this);
    }
}
