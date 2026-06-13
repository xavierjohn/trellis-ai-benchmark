namespace OrderManagement.Domain;

/// <summary>
/// A line item entity within an order.
/// </summary>
public class LineItem : Entity<LineItemId>
{
    /// <summary>The product being ordered.</summary>
    public ProductId ProductId { get; private set; } = null!;

    /// <summary>Snapshot of the product name at time of order.</summary>
    public ProductName ProductName { get; private set; } = null!;

    /// <summary>Quantity ordered.</summary>
    public Quantity Quantity { get; private set; } = null!;

    /// <summary>Snapshot of the unit price at time of order.</summary>
    public UnitPrice UnitPrice { get; private set; } = null!;

    /// <summary>EF Core constructor.</summary>
    private LineItem() : base(default!) { }

    /// <summary>Creates a new line item.</summary>
    public LineItem(
        ProductId productId,
        ProductName productName,
        Quantity quantity,
        UnitPrice unitPrice) : base(LineItemId.NewUniqueV7())
    {
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}
