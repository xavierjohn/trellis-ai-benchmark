namespace OrderManagement.Domain;

/// <summary>
/// Order line item entity.
/// </summary>
public class LineItem : Entity<LineItemId>
{
    /// <summary>Referenced product identifier.</summary>
    public ProductId ProductId { get; private set; } = null!;

    /// <summary>Snapshot of product name.</summary>
    public string ProductName { get; private set; } = null!;

    /// <summary>Ordered quantity.</summary>
    public int Quantity { get; private set; }

    /// <summary>Snapshot of unit price.</summary>
    public decimal UnitPrice { get; private set; }

    private LineItem() : base(default!)
    {
    }

    /// <summary>
    /// Creates an order line item snapshot.
    /// </summary>
    public LineItem(ProductId productId, string productName, int quantity, decimal unitPrice)
        : base(LineItemId.NewUniqueV7())
    {
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    /// <summary>Total line price.</summary>
    public decimal Total => UnitPrice * Quantity;
}
