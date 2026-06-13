namespace OrderManagement.Domain;

/// <summary>
/// Unit price of a product, in USD. Must be greater than zero.
/// </summary>
public partial class UnitPrice : RequiredDecimal<UnitPrice>
{
    static partial void ValidateAdditional(decimal value, string fieldName, ref string? errorMessage)
    {
        if (value < 0m)
            errorMessage = "Unit price must be greater than zero.";
    }
}

/// <summary>
/// Quantity of a product in a line item. Between 1 and 999 inclusive.
/// </summary>
[Range(1, 999)]
public partial class Quantity : RequiredInt<Quantity>
{
}
