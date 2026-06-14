namespace Application.Products;

public record AddStockCommand(Guid ProductId, int Quantity);
