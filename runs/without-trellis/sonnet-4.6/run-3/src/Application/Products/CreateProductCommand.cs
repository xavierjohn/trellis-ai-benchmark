namespace Application.Products;

public record CreateProductCommand(
    string ProductName,
    string SKU,
    decimal UnitPrice,
    int StockQuantity = 0);
