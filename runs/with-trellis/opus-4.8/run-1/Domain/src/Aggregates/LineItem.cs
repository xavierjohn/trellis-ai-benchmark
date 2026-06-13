namespace OrderManagement.Domain;

/// <summary>
/// A single entry in an order specifying a product, quantity, and the unit price captured
/// at the time the item was added.
/// </summary>
public class LineItem : Entity<LineItemId>
{
    /// <summary>Referenced product.</summary>
    public ProductId ProductId { get; private set; } = null!;

    /// <summary>Snapshot of the product name at the time of ordering.</summary>
    public ProductName ProductName { get; private set; } = null!;

    /// <summary>Quantity ordered (1–999).</summary>
    public Quantity Quantity { get; private set; } = null!;

    /// <summary>Snapshot of the product unit price at the time of ordering.</summary>
    public UnitPrice UnitPrice { get; private set; } = null!;

    /// <summary>EF Core constructor.</summary>
    private LineItem() : base(default!)
    {
    }

    internal LineItem(ProductId productId, ProductName productName, Quantity quantity, UnitPrice unitPrice)
        : base(LineItemId.NewUniqueV7())
    {
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    /// <summary>The line total: unit price multiplied by quantity.</summary>
    public decimal LineTotal => UnitPrice.Value * Quantity.Value;
}
