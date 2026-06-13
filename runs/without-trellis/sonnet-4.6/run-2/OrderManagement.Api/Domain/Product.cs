namespace OrderManagement.Api.Domain;

public class Product
{
    public Guid Id { get; private set; }
    public string ProductName { get; private set; } = "";
    public string SKU { get; private set; } = "";
    public decimal UnitPrice { get; private set; }
    public int StockQuantity { get; private set; }

    private Product() { } // EF Core

    public static Product Create(string productName, string sku, decimal unitPrice)
    {
        return new Product
        {
            Id = Guid.NewGuid(),
            ProductName = productName,
            SKU = sku,
            UnitPrice = unitPrice,
            StockQuantity = 0
        };
    }

    public void AddStock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainValidationException("Quantity must be positive.");
        StockQuantity += quantity;
    }

    public void ReserveStock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainValidationException("Quantity must be positive.");
        if (StockQuantity < quantity)
            throw new DomainValidationException(
                $"Insufficient stock for product '{ProductName}'. Available: {StockQuantity}, requested: {quantity}.");
        StockQuantity -= quantity;
    }

    public void ReleaseStock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainValidationException("Quantity must be positive.");
        StockQuantity += quantity;
    }
}
