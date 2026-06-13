namespace OrderManagement.Domain;

/// <summary>Order line item entity.</summary>
public class OrderLineItem : Entity<LineItemId>
{
    /// <summary>Referenced product id.</summary>
    public ProductId ProductId { get; private set; } = null!;

    /// <summary>Product name snapshot.</summary>
    public ProductName ProductName { get; private set; } = null!;

    /// <summary>Quantity ordered.</summary>
    public OrderQuantity Quantity { get; private set; } = null!;

    /// <summary>Unit price snapshot.</summary>
    public UnitPrice UnitPrice { get; private set; } = null!;

    private OrderLineItem() : base(default!)
    {
    }

    /// <summary>Create a line item from a product snapshot.</summary>
    public OrderLineItem(Product product, OrderQuantity quantity)
        : base(LineItemId.NewUniqueV7())
    {
        ProductId = product.Id;
        ProductName = product.ProductName;
        Quantity = quantity;
        UnitPrice = product.UnitPrice;
    }

    /// <summary>Line total.</summary>
    public decimal LineTotal => UnitPrice.Value * Quantity.Value;
}
