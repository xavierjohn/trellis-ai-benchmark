namespace OrderManagement.Domain;

/// <summary>Order line item entity.</summary>
public class OrderLineItem : Entity<LineItemId>
{
    private OrderLineItem() : base(default!)
    {
        ProductId = null!;
        ProductName = null!;
        Quantity = null!;
        UnitPrice = null!;
    }

    internal OrderLineItem(Product product, OrderQuantity quantity)
        : base(LineItemId.NewUniqueV7())
    {
        ProductId = product.Id;
        ProductName = product.Name;
        Quantity = quantity;
        UnitPrice = product.UnitPrice;
    }

    /// <summary>Product identifier.</summary>
    public ProductId ProductId { get; private set; }

    /// <summary>Product name snapshot.</summary>
    public ProductName ProductName { get; private set; }

    /// <summary>Quantity ordered.</summary>
    public OrderQuantity Quantity { get; private set; }

    /// <summary>Unit price snapshot.</summary>
    public UnitPrice UnitPrice { get; private set; }

    /// <summary>Line total.</summary>
    public decimal Total => UnitPrice.Value * Quantity.Value;
}
