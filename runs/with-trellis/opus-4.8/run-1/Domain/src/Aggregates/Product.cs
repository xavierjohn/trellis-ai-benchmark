namespace OrderManagement.Domain;

/// <summary>
/// A product available for purchase. Identified by <see cref="ProductId"/>.
/// </summary>
public partial class Product : Aggregate<ProductId>
{
    /// <summary>Product name.</summary>
    public ProductName Name { get; private set; } = null!;

    /// <summary>Stock Keeping Unit. Unique across all products.</summary>
    public Sku Sku { get; private set; } = null!;

    /// <summary>Unit price in USD.</summary>
    public UnitPrice UnitPrice { get; private set; } = null!;

    /// <summary>Current available stock quantity. Never negative.</summary>
    public int StockQuantity { get; private set; }

    /// <summary>EF Core constructor.</summary>
    private Product() : base(default!)
    {
    }

    /// <summary>
    /// Creates a new product with zero initial stock.
    /// </summary>
    public Product(ProductName name, Sku sku, UnitPrice unitPrice)
        : base(ProductId.NewUniqueV7())
    {
        Name = name;
        Sku = sku;
        UnitPrice = unitPrice;
        StockQuantity = 0;
    }

    /// <summary>
    /// Increases stock quantity. Quantity must be positive.
    /// </summary>
    public Result<Product> AddStock(int quantity) =>
        Result.Ensure(
                quantity > 0,
                Error.InvalidInput.ForField("quantity", "out_of_range", "Quantity must be positive."))
            .Tap(() => StockQuantity += quantity)
            .Map(_ => this);

    /// <summary>
    /// Pure predicate: returns success when <paramref name="quantity"/> can be reserved from current stock.
    /// </summary>
    public Result<Unit> CanReserve(int quantity) =>
        Result.Ensure(
            quantity > 0 && quantity <= StockQuantity,
            Error.InvalidInput.ForRule(
                "stock.insufficient",
                $"Cannot reserve {quantity} unit(s) of '{Name.Value}' from stock of {StockQuantity}."));

    /// <summary>
    /// Decreases stock quantity by <paramref name="quantity"/>. Fails if insufficient stock.
    /// </summary>
    public Result<Unit> ReserveStock(int quantity) =>
        CanReserve(quantity).Tap(() => StockQuantity -= quantity);

    /// <summary>
    /// Restores previously-reserved stock when an order is cancelled.
    /// </summary>
    public void ReleaseStock(int quantity) => StockQuantity += quantity;
}
