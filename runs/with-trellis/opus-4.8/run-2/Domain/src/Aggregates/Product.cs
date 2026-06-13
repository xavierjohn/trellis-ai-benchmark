namespace OrderManagement.Domain;

using Trellis.Primitives;

/// <summary>
/// A product available for purchase, with inventory tracking.
/// </summary>
public partial class Product : Aggregate<ProductId>
{
    /// <summary>Product name.</summary>
    public ProductName Name { get; private set; } = null!;

    /// <summary>Stock keeping unit (unique across all products).</summary>
    public Sku Sku { get; private set; } = null!;

    /// <summary>Unit price (USD).</summary>
    public MonetaryAmount UnitPrice { get; private set; } = null!;

    /// <summary>Current available stock quantity (never negative).</summary>
    public int StockQuantity { get; private set; }

    /// <summary>EF Core constructor.</summary>
    private Product() : base(default!) { }

    private Product(ProductId id, ProductName name, Sku sku, MonetaryAmount unitPrice, int stockQuantity)
        : base(id)
    {
        Name = name;
        Sku = sku;
        UnitPrice = unitPrice;
        StockQuantity = stockQuantity;
    }

    /// <summary>
    /// Creates a new product. Unit price must be greater than zero.
    /// </summary>
    public static Result<Product> TryCreate(ProductName name, Sku sku, MonetaryAmount unitPrice) =>
        Result.Ensure(
                unitPrice.Value > 0m,
                Error.InvalidInput.ForField("unitPrice", "out_of_range", "Unit price must be greater than zero."))
            .Map(_ => new Product(ProductId.NewUniqueV7(), name, sku, unitPrice, stockQuantity: 0));

    /// <summary>
    /// Increases stock quantity by a positive amount.
    /// </summary>
    public Result<Product> AddStock(StockAddition quantity)
    {
        StockQuantity += quantity.Value;
        return Result.Ok(this);
    }

    /// <summary>
    /// Pure predicate: whether the given quantity can be reserved from current stock.
    /// </summary>
    public Result<Unit> CanReserveStock(int quantity) =>
        Result.Ensure(
            quantity > 0 && quantity <= StockQuantity,
            Error.InvalidInput.ForRule(
                "stock.insufficient",
                $"Cannot reserve {quantity} unit(s) of SKU {Sku.Value} from stock of {StockQuantity}."));

    /// <summary>
    /// Reserves (decrements) stock. Re-checks <see cref="CanReserveStock"/> as defense in depth.
    /// </summary>
    public Result<Unit> ReserveStock(int quantity) =>
        CanReserveStock(quantity).Tap(() => StockQuantity -= quantity);

    /// <summary>
    /// Releases (restores) previously reserved stock.
    /// </summary>
    public Result<Product> ReleaseStock(int quantity)
    {
        StockQuantity += quantity;
        return Result.Ok(this);
    }
}
