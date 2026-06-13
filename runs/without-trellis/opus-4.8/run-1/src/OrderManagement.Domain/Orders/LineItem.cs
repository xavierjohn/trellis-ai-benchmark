namespace OrderManagement.Domain.Orders;

/// <summary>A single line in an order. Captures product price/name at the time of ordering.</summary>
public sealed class LineItem
{
    public const int MinQuantity = 1;
    public const int MaxQuantity = 999;

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = null!;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    public decimal LineTotal => UnitPrice * Quantity;

    private LineItem() { } // EF

    internal LineItem(Guid productId, string productName, int quantity, decimal unitPrice)
    {
        Id = Guid.NewGuid();
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}
