using Domain.Common;
using System.Text.RegularExpressions;

namespace Domain.Products;

public class Product
{
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public string SKU { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public int StockQuantity { get; private set; }

    private Product()
    {
    }

    public static Result<Product> Create(string productName, string sku, decimal unitPrice, int stockQuantity = 0)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(productName) || productName.Length > 200)
        {
            errors.Add("ProductName must be 1-200 characters.");
        }

        if (string.IsNullOrWhiteSpace(sku) || sku.Length < 3 || sku.Length > 20 || !Regex.IsMatch(sku, @"^[A-Z0-9]+$"))
        {
            errors.Add("SKU must be 3-20 uppercase alphanumeric characters.");
        }

        if (unitPrice <= 0)
        {
            errors.Add("UnitPrice must be greater than 0.");
        }

        if (stockQuantity < 0)
        {
            errors.Add("StockQuantity must be non-negative.");
        }

        if (errors.Count > 0)
        {
            return Result<Product>.Failure(string.Join(" ", errors));
        }

        return Result<Product>.Success(new Product
        {
            ProductId = Guid.NewGuid(),
            ProductName = productName.Trim(),
            SKU = sku.Trim(),
            UnitPrice = unitPrice,
            StockQuantity = stockQuantity
        });
    }

    public Result AddStock(int qty)
    {
        if (qty <= 0)
        {
            return Result.Failure("Quantity to add must be positive.");
        }

        StockQuantity += qty;
        return Result.Success();
    }

    public Result ReserveStock(int qty)
    {
        if (qty <= 0)
        {
            return Result.Failure("Quantity to reserve must be positive.");
        }

        if (qty > StockQuantity)
        {
            return Result.Failure($"Insufficient stock. Available: {StockQuantity}, Requested: {qty}.", "unprocessable");
        }

        StockQuantity -= qty;
        return Result.Success();
    }

    public void ReleaseStock(int qty)
    {
        StockQuantity += qty;
    }
}
