namespace OrderManagement.Domain;

using Trellis.Primitives;

/// <summary>
/// Product aggregate with inventory.
/// </summary>
public class Product : Aggregate<ProductId>
{
    /// <summary>Product name.</summary>
    public ProductName Name { get; private set; } = null!;

    /// <summary>Unique SKU.</summary>
    public Sku Sku { get; private set; } = null!;

    /// <summary>Unit price in USD.</summary>
    public MonetaryAmount UnitPrice { get; private set; } = null!;

    /// <summary>Available stock.</summary>
    public StockQuantity StockQuantity { get; private set; } = null!;

    private Product() : base(default!)
    {
    }

    private Product(ProductName name, Sku sku, MonetaryAmount unitPrice)
        : base(ProductId.NewUniqueV7())
    {
        Name = name;
        Sku = sku;
        UnitPrice = unitPrice;
        StockQuantity = Domain.StockQuantity.Create(0);
    }

    /// <summary>
    /// Creates a product and enforces positive price.
    /// </summary>
    public static Result<Product> TryCreate(ProductName name, Sku sku, MonetaryAmount unitPrice) =>
        Result.Ensure(unitPrice.Value > 0m, Error.InvalidInput.ForField("unitPrice", "greater_than_zero", "Unit price must be greater than zero."))
            .Map(_ => new Product(name, sku, unitPrice));

    /// <summary>
    /// Adds available stock.
    /// </summary>
    public Result<Product> AddStock(StockAdjustmentQuantity quantity) =>
        SetStock(StockQuantity.Value + quantity.Value);

    /// <summary>
    /// Reserves stock for an order line.
    /// </summary>
    public Result<Product> ReserveStock(LineItemQuantity quantity) =>
        StockQuantity.Value < quantity.Value
            ? Result.Fail<Product>(Error.InvalidInput.ForRule("product.insufficient_stock", $"Insufficient stock for product {Id}."))
            : SetStock(StockQuantity.Value - quantity.Value);

    /// <summary>
    /// Releases previously reserved stock.
    /// </summary>
    public Result<Product> ReleaseStock(LineItemQuantity quantity) =>
        SetStock(StockQuantity.Value + quantity.Value);

    /// <summary>
    /// Returns true when the product has at least the requested stock available.
    /// </summary>
    public bool HasAvailableStock(LineItemQuantity quantity) => StockQuantity.Value >= quantity.Value;

    private Result<Product> SetStock(int value) =>
        Domain.StockQuantity.TryCreate(value, "stockQuantity")
            .Map(stock =>
            {
                StockQuantity = stock;
                return this;
            });
}
