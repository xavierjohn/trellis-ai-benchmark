namespace OrderManagement.Domain;

/// <summary>Unit price of a product, in USD. Must be greater than zero.</summary>
[Positive]
public partial class UnitPrice : RequiredDecimal<UnitPrice>
{
    static partial void ValidateAdditional(decimal value, string fieldName, ref string? errorMessage)
    {
        if (value <= 0m)
            errorMessage = "Unit price must be greater than zero.";
    }
}

/// <summary>Quantity of a line item. Between 1 and 999 inclusive.</summary>
[Range(1, 999)]
public partial class Quantity : RequiredInt<Quantity>
{
    static partial void ValidateAdditional(int value, string fieldName, ref string? errorMessage)
    {
        if (value < 1 || value > 999)
            errorMessage = "Quantity must be between 1 and 999.";
    }
}

/// <summary>Available stock quantity for a product. Non-negative.</summary>
[AllowZero]
[NonNegative]
public partial class StockQuantity : RequiredInt<StockQuantity>
{
    static partial void ValidateAdditional(int value, string fieldName, ref string? errorMessage)
    {
        if (value < 0)
            errorMessage = "Stock quantity cannot be negative.";
    }
}
