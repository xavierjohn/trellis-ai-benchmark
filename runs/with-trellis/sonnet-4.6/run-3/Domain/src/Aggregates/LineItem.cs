namespace OrderManagement.Domain;

/// <summary>A line item within an order.</summary>
public class LineItem : Entity<LineItemId>
{
    /// <summary>Product reference.</summary>
    public ProductId ProductId { get; private set; } = null!;

    /// <summary>Snapshot of product name at order time.</summary>
    public ProductName ProductName { get; private set; } = null!;

    /// <summary>Quantity ordered.</summary>
    public int Quantity { get; private set; }

    /// <summary>Unit price at time of ordering.</summary>
    public UnitPrice UnitPrice { get; private set; } = null!;

    /// <summary>EF Core constructor.</summary>
    private LineItem()
        : base(default!)
    {
    }

    /// <summary>Creates a new line item.</summary>
    public LineItem(ProductId productId, ProductName productName, int quantity, UnitPrice unitPrice)
        : base(LineItemId.NewUniqueV7())
    {
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    /// <summary>Line item total.</summary>
    public decimal Total => UnitPrice.Value * Quantity;
}
