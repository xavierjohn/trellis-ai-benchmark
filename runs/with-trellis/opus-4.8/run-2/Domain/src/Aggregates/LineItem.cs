namespace OrderManagement.Domain;

using Trellis.Primitives;

/// <summary>
/// A single line in an order: a product, a quantity, and the unit price captured
/// at the time the item was added to the order.
/// </summary>
public partial class LineItem : Entity<LineItemId>
{
    /// <summary>The referenced product.</summary>
    public ProductId ProductId { get; private set; } = null!;

    /// <summary>Snapshot of the product name at the time of ordering.</summary>
    public ProductName ProductName { get; private set; } = null!;

    /// <summary>Quantity ordered (1–999).</summary>
    public Quantity Quantity { get; private set; } = null!;

    /// <summary>Snapshot of the unit price at the time of ordering.</summary>
    public MonetaryAmount UnitPrice { get; private set; } = null!;

    /// <summary>EF Core constructor.</summary>
    private LineItem() : base(default!) { }

    private LineItem(LineItemId id, ProductId productId, ProductName productName, Quantity quantity, MonetaryAmount unitPrice)
        : base(id)
    {
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    /// <summary>
    /// Creates a new line item with a fresh identity.
    /// </summary>
    public static LineItem Create(ProductId productId, ProductName productName, Quantity quantity, MonetaryAmount unitPrice) =>
        new(LineItemId.NewUniqueV7(), productId, productName, quantity, unitPrice);

    /// <summary>The line total (unit price × quantity).</summary>
    public MonetaryAmount LineTotal => MonetaryAmount.Create(UnitPrice.Value * Quantity.Value);
}
