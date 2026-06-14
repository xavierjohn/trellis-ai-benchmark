namespace OrderManagement.Domain;

using Trellis.Primitives;

/// <summary>
/// Snapshot of a product line in an order.
/// </summary>
public class OrderLineItem : Entity<LineItemId>
{
    /// <summary>Referenced product.</summary>
    public ProductId ProductId { get; private set; } = null!;

    /// <summary>Product name captured when the line item was added.</summary>
    public ProductName ProductName { get; private set; } = null!;

    /// <summary>Ordered quantity.</summary>
    public LineItemQuantity Quantity { get; private set; } = null!;

    /// <summary>Unit price captured when the line item was added.</summary>
    public MonetaryAmount UnitPrice { get; private set; } = null!;

    private OrderLineItem() : base(default!)
    {
    }

    /// <summary>
    /// Creates a line-item snapshot from product state.
    /// </summary>
    public OrderLineItem(Product product, LineItemQuantity quantity)
        : base(LineItemId.NewUniqueV7())
    {
        ProductId = product.Id;
        ProductName = product.Name;
        Quantity = quantity;
        UnitPrice = product.UnitPrice;
    }

    /// <summary>
    /// Calculates the line total.
    /// </summary>
    public Money GetTotal() => Money.Create(UnitPrice.Value * Quantity.Value, "USD");
}
