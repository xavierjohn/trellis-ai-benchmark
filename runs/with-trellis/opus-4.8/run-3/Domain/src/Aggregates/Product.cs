namespace OrderManagement.Domain;

/// <summary>A product available for purchase.</summary>
public sealed class Product : Aggregate<ProductId>
{
    /// <summary>The product name.</summary>
    public ProductName Name { get; private set; } = null!;

    /// <summary>The unique stock-keeping unit.</summary>
    public Sku Sku { get; private set; } = null!;

    /// <summary>The unit price in USD.</summary>
    public UnitPrice UnitPrice { get; private set; } = null!;

    /// <summary>The currently available stock quantity.</summary>
    public StockQuantity StockQuantity { get; private set; } = null!;

    private Product() : base(default!)
    {
    }

    private Product(ProductName name, Sku sku, UnitPrice unitPrice, StockQuantity stockQuantity)
        : base(ProductId.NewUniqueV7())
    {
        Name = name;
        Sku = sku;
        UnitPrice = unitPrice;
        StockQuantity = stockQuantity;
    }

    /// <summary>Creates a new product with the given starting stock (defaults to zero).</summary>
    public static Product Create(ProductName name, Sku sku, UnitPrice unitPrice, StockQuantity? stockQuantity = null) =>
        new(name, sku, unitPrice, stockQuantity ?? StockQuantity.Create(0));

    /// <summary>Increases stock. The quantity to add must be positive.</summary>
    public Result<Product> AddStock(int quantity)
    {
        if (quantity <= 0)
            return Result.Fail<Product>(
                Error.InvalidInput.ForField("quantity", "out_of_range", "Quantity to add must be positive."));

        StockQuantity = StockQuantity.Create(StockQuantity.Value + quantity);
        return Result.Ok(this);
    }

    /// <summary>Checks whether the requested quantity can be reserved without mutating stock.</summary>
    public Result<Unit> CanReserveStock(Quantity quantity) =>
        StockQuantity.Value >= quantity.Value
            ? Result.Ok(Unit.Value)
            : Result.Fail<Unit>(Error.InvalidInput.ForRule(
                "product.insufficient_stock",
                $"Insufficient stock for product '{Name.Value}'. Available {StockQuantity.Value}, requested {quantity.Value}."));

    /// <summary>Reserves (decreases) stock. Fails if there is insufficient stock.</summary>
    public Result<Product> ReserveStock(Quantity quantity) =>
        CanReserveStock(quantity)
            .Tap(() => StockQuantity = StockQuantity.Create(StockQuantity.Value - quantity.Value))
            .Map(_ => this);

    /// <summary>Releases (restores) previously reserved stock.</summary>
    public Result<Product> ReleaseStock(Quantity quantity)
    {
        StockQuantity = StockQuantity.Create(StockQuantity.Value + quantity.Value);
        return Result.Ok(this);
    }
}
