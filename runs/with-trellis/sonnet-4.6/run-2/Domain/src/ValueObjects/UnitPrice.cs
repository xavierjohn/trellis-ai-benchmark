namespace OrderManagement.Domain;

/// <summary>Unit price of a product — must be greater than zero.</summary>
public partial class UnitPrice : RequiredDecimal<UnitPrice>
{
    static partial void ValidateAdditional(decimal value, string fieldName, ref string? errorMessage)
    {
        if (value <= 0)
            errorMessage = $"{fieldName} must be greater than zero.";
    }
}
