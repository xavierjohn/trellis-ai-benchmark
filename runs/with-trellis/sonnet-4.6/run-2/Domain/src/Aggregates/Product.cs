namespace OrderManagement.Domain;

/// <summary>
/// A product aggregate.
/// </summary>
public partial class Product : Aggregate<ProductId>
{
    /// <summary>Product name.</summary>
    public ProductName ProductName { get; private set; } = null!;

    /// <summary>Stock-keeping unit.</summary>
    public SKU SKU { get; private set; } = null!;

    /// <summary>Price per unit.</summary>
    public UnitPrice UnitPrice { get; private set; } = null!;

    /// <summary>Current stock quantity.</summary>
    public StockQuantity StockQuantity { get; private set; } = null!;

    /// <summary>EF Core constructor.</summary>
    private Product() : base(default!) { }

    /// <summary>Creates a new product with zero stock.</summary>
    public Product(ProductName productName, SKU sku, UnitPrice unitPrice) : base(ProductId.NewUniqueV7())
    {
        ProductName = productName;
        SKU = sku;
        UnitPrice = unitPrice;
        StockQuantity = StockQuantity.Create(0);
    }

    /// <summary>Adds stock to this product.</summary>
    public Result<Unit> AddStock(Quantity quantity) =>
        Result.Ensure(quantity.Value > 0,
            Error.InvalidInput.ForField("quantity", "positive", "Quantity must be positive."))
        .Tap(_ => StockQuantity = StockQuantity.Create(StockQuantity.Value + quantity.Value));

    /// <summary>Reserves stock for an order. Fails if insufficient stock is available.</summary>
    public Result<Unit> ReserveStock(Quantity quantity) =>
        Result.Ensure(StockQuantity.Value >= quantity.Value,
            Error.InvalidInput.ForRule("insufficient_stock",
                $"Insufficient stock for product {SKU.Value}. Available: {StockQuantity.Value}, requested: {quantity.Value}."))
        .Tap(_ => StockQuantity = StockQuantity.Create(StockQuantity.Value - quantity.Value));

    /// <summary>Releases previously reserved stock back to available.</summary>
    public void ReleaseStock(Quantity quantity) =>
        StockQuantity = StockQuantity.Create(StockQuantity.Value + quantity.Value);
}
