namespace OrderManagement.Domain;

/// <summary>Product aggregate.</summary>
public partial class Product : Aggregate<ProductId>
{
    public ProductName ProductName { get; private set; } = null!;
    public Sku Sku { get; private set; } = null!;
    public UnitPrice UnitPrice { get; private set; } = null!;
    public int StockQuantity { get; private set; }

    /// <summary>EF Core constructor.</summary>
    private Product()
        : base(default!)
    {
    }

    /// <summary>Creates a new product.</summary>
    public Product(ProductName productName, Sku sku, UnitPrice unitPrice)
        : base(ProductId.NewUniqueV7())
    {
        ProductName = productName;
        Sku = sku;
        UnitPrice = unitPrice;
        StockQuantity = 0;
    }

    /// <summary>Adds stock quantity. Quantity must be positive.</summary>
    public Result<Product> AddStock(int quantity)
    {
        if (quantity <= 0)
            return Result.Fail<Product>(Error.InvalidInput.ForField("quantity", "positive", "Quantity must be positive."));

        StockQuantity += quantity;
        return Result.Ok(this);
    }

    /// <summary>Reserves (reduces) stock. Fails if insufficient stock.</summary>
    public Result<Product> ReserveStock(int quantity)
    {
        if (quantity <= 0)
            return Result.Fail<Product>(Error.InvalidInput.ForField("quantity", "positive", "Quantity must be positive."));

        if (StockQuantity < quantity)
            return Result.Fail<Product>(Error.InvalidInput.ForRule("stock.insufficient", $"Insufficient stock for product '{ProductName.Value}'. Available: {StockQuantity}, Requested: {quantity}."));

        StockQuantity -= quantity;
        return Result.Ok(this);
    }

    /// <summary>Releases stock back (used on order cancellation).</summary>
    public void ReleaseStock(int quantity) => StockQuantity += quantity;
}
