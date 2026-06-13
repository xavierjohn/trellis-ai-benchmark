namespace OrderManagement.Domain.Aggregates;

public class LineItem
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = default!;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    private LineItem() { }

    public static LineItem Create(Guid orderId, Guid productId, string productName, int quantity, decimal unitPrice)
    {
        return new LineItem
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ProductId = productId,
            ProductName = productName,
            Quantity = quantity,
            UnitPrice = unitPrice
        };
    }

    public decimal LineTotal => UnitPrice * Quantity;
}
