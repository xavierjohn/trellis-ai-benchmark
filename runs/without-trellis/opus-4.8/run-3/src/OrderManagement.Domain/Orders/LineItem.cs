namespace OrderManagement.Domain.Orders;

/// <summary>A single line in an order. Price and product name are snapshots taken at order time.</summary>
public sealed class LineItem
{
    internal LineItem(Guid id, Guid productId, string productName, int quantity, decimal unitPrice)
    {
        Id = id;
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    // Parameterless ctor for EF Core materialization.
    private LineItem() { }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    public decimal LineTotal => UnitPrice * Quantity;

    internal void IncreaseQuantity(int by) => Quantity += by;
}
