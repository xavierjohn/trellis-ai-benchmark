namespace OrderManagement.Domain;

/// <summary>
/// Product aggregate.
/// </summary>
public partial class Product : Aggregate<ProductId>
{
    /// <summary>Product name.</summary>
    public ProductName Name { get; private set; } = null!;

    /// <summary>Product SKU.</summary>
    public Sku Sku { get; private set; } = null!;

    /// <summary>Unit price.</summary>
    public decimal UnitPrice { get; private set; }

    /// <summary>Available stock quantity.</summary>
    public int StockQuantity { get; private set; }

    private Product() : base(default!)
    {
    }

    /// <summary>
    /// Creates a product with zero starting stock.
    /// </summary>
    public Product(ProductName name, Sku sku, decimal unitPrice)
        : base(ProductId.NewUniqueV7())
    {
        Name = name;
        Sku = sku;
        UnitPrice = unitPrice;
        StockQuantity = 0;
    }

    /// <summary>
    /// Adds stock.
    /// </summary>
    public Result<Unit> AddStock(int quantity)
    {
        if (quantity <= 0)
            return Result.Fail(Error.InvalidInput.ForField("quantity", "positive", "Quantity to add must be positive."));

        StockQuantity += quantity;
        return Result.Ok();
    }

    /// <summary>
    /// Reserves stock for an order submission.
    /// </summary>
    public Result<Unit> ReserveStock(int quantity)
    {
        if (quantity <= 0)
            return Result.Fail(Error.InvalidInput.ForField("quantity", "positive", "Quantity to reserve must be positive."));

        if (StockQuantity < quantity)
            return Result.Fail(Error.InvalidInput.ForField("quantity", "insufficient_stock", $"Insufficient stock for product {Name.Value}. Available: {StockQuantity}, requested: {quantity}."));

        StockQuantity -= quantity;
        return Result.Ok();
    }

    /// <summary>
    /// Releases previously reserved stock.
    /// </summary>
    public void ReleaseStock(int quantity) => StockQuantity += quantity;
}
