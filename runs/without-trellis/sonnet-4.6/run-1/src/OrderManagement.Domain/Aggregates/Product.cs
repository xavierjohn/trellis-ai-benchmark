using OrderManagement.Domain.Exceptions;
using System.Text.RegularExpressions;

namespace OrderManagement.Domain.Aggregates;

public class Product
{
    public Guid Id { get; private set; }
    public string ProductName { get; private set; } = default!;
    public string SKU { get; private set; } = default!;
    public decimal UnitPrice { get; private set; }
    public int StockQuantity { get; private set; }

    private Product() { }

    public static Product Create(string productName, string sku, decimal unitPrice)
    {
        var errors = new List<string>();

        // Normalize SKU before validation
        var normalizedSku = sku?.Trim().ToUpperInvariant() ?? "";

        if (string.IsNullOrWhiteSpace(productName) || productName.Length > 200)
            errors.Add("ProductName must be between 1 and 200 characters.");
        if (!IsValidSku(normalizedSku))
            errors.Add("SKU must be 3–20 uppercase alphanumeric characters.");
        if (unitPrice <= 0)
            errors.Add("UnitPrice must be greater than zero.");

        if (errors.Count > 0)
            throw new ValidationException(errors);

        return new Product
        {
            Id = Guid.NewGuid(),
            ProductName = productName.Trim(),
            SKU = normalizedSku,
            UnitPrice = unitPrice,
            StockQuantity = 0
        };
    }

    public void AddStock(int quantity)
    {
        if (quantity <= 0)
            throw new ValidationException("Quantity must be a positive integer.");
        StockQuantity += quantity;
    }

    public void ReserveStock(int quantity)
    {
        if (quantity <= 0)
            throw new ValidationException("Quantity must be a positive integer.");
        if (StockQuantity < quantity)
            throw new ValidationException(
                $"Insufficient stock for product '{ProductName}'. Available: {StockQuantity}, requested: {quantity}.");
        StockQuantity -= quantity;
    }

    public void ReleaseStock(int quantity)
    {
        if (quantity <= 0)
            throw new ValidationException("Quantity must be a positive integer.");
        StockQuantity += quantity;
    }

    private static bool IsValidSku(string? sku)
    {
        if (string.IsNullOrWhiteSpace(sku)) return false;
        return Regex.IsMatch(sku.Trim(), @"^[A-Z0-9]{3,20}$");
    }
}
