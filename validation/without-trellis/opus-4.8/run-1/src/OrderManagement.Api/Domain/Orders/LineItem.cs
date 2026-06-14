namespace OrderManagement.Api.Domain.Orders;

/// <summary>A single line item within an order. Unit price is snapshotted at add time.</summary>
public sealed class LineItem
{
    private LineItem() { } // EF

    internal LineItem(Guid id, Guid productId, string productName, int quantity, decimal unitPrice)
    {
        Id = id;
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = default!;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    public decimal LineTotal => UnitPrice * Quantity;
}
