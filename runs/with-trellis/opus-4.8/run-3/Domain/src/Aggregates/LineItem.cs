namespace OrderManagement.Domain;

/// <summary>A single line in an order: a product, a quantity, and the unit price at order time.</summary>
public sealed class LineItem : Entity<LineItemId>
{
    /// <summary>The product referenced by this line item.</summary>
    public ProductId ProductId { get; private set; } = null!;

    /// <summary>Snapshot of the product name at the time the line item was added.</summary>
    public ProductName ProductName { get; private set; } = null!;

    /// <summary>Quantity ordered (1–999).</summary>
    public Quantity Quantity { get; private set; } = null!;

    /// <summary>Snapshot of the product unit price at the time the line item was added.</summary>
    public UnitPrice UnitPrice { get; private set; } = null!;

    /// <summary>The line total (unit price × quantity).</summary>
    public decimal LineTotal => UnitPrice.Value * Quantity.Value;

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
}
