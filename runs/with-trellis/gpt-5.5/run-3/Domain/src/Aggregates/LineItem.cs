namespace OrderManagement.Domain;

/// <summary>
/// Order line item entity.
/// </summary>
public class LineItem : Entity<LineItemId>
{
    /// <summary>Referenced product.</summary>
    public ProductId ProductId { get; private set; } = null!;

    /// <summary>Product name snapshot.</summary>
    public ProductName ProductName { get; private set; } = null!;

    /// <summary>Quantity ordered.</summary>
    public LineItemQuantity Quantity { get; private set; } = null!;

    /// <summary>Unit price snapshot.</summary>
    public UnitPrice UnitPrice { get; private set; } = null!;

    private LineItem() : base(default!)
    {
    }

    internal LineItem(Product product, LineItemQuantity quantity, TimeProvider timeProvider)
        : base(LineItemId.NewUniqueV7(timeProvider))
    {
        ProductId = product.Id;
        ProductName = product.ProductName;
        Quantity = quantity;
        UnitPrice = product.UnitPrice;
    }

    /// <summary>Line total.</summary>
    public decimal Total => UnitPrice.Value * Quantity.Value;
}
