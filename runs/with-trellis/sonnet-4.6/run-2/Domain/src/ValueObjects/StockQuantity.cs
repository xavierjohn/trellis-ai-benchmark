namespace OrderManagement.Domain;

/// <summary>Stock quantity — must be zero or positive.</summary>
[AllowZero]
public partial class StockQuantity : RequiredInt<StockQuantity>
{
    static partial void ValidateAdditional(int value, string fieldName, ref string? errorMessage)
    {
        if (value < 0)
            errorMessage = $"{fieldName} must be zero or positive.";
    }
}
