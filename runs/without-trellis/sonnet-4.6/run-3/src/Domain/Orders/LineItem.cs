namespace Domain.Orders;

public class LineItem
{
    public Guid LineItemId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    private LineItem()
    {
    }

    internal static LineItem Create(Guid productId, string productName, int quantity, decimal unitPrice)
    {
        return new LineItem
        {
            LineItemId = Guid.NewGuid(),
            ProductId = productId,
            ProductName = productName,
            Quantity = quantity,
            UnitPrice = unitPrice
        };
    }
}
