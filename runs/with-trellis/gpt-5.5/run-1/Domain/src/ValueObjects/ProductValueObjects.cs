namespace OrderManagement.Domain;

using System.Text.RegularExpressions;

/// <summary>
/// Product display name.
/// </summary>
[StringLength(200, MinimumLength = 1)]
public partial class ProductName : RequiredString<ProductName>
{
}

/// <summary>
/// Stock keeping unit. Uppercase letters and digits only.
/// </summary>
[StringLength(20, MinimumLength = 3)]
public partial class Sku : RequiredString<Sku>
{
    private static readonly Regex Pattern = new("^[A-Z0-9]{3,20}$", RegexOptions.Compiled);

    static partial void ValidateAdditional(string value, string fieldName, ref string? errorMessage)
    {
        if (!Pattern.IsMatch(value))
            errorMessage = "SKU must contain only uppercase letters and digits.";
    }
}

/// <summary>
/// Available inventory count for a product.
/// </summary>
[AllowZero]
[Range(0, int.MaxValue)]
public partial class StockQuantity : RequiredInt<StockQuantity>
{
}

/// <summary>
/// Positive stock adjustment quantity.
/// </summary>
[Range(1, int.MaxValue)]
public partial class StockAdjustmentQuantity : RequiredInt<StockAdjustmentQuantity>
{
}

/// <summary>
/// Quantity for one order line item.
/// </summary>
[Range(1, 999)]
public partial class LineItemQuantity : RequiredInt<LineItemQuantity>
{
}
