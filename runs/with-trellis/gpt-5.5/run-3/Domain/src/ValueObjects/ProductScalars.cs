namespace OrderManagement.Domain;

using System.Text.RegularExpressions;

/// <summary>Stock keeping unit.</summary>
[StringLength(20, MinimumLength = 3)]
public partial class Sku : RequiredString<Sku>
{
    private static readonly Regex Pattern = new("^[A-Z0-9]{3,20}$", RegexOptions.Compiled);

    static partial void ValidateAdditional(string value, string fieldName, ref string? errorMessage)
    {
        if (!Pattern.IsMatch(value))
            errorMessage = "SKU must contain 3-20 uppercase letters or digits.";
    }
}

/// <summary>Unit price in USD.</summary>
[Positive]
public partial class UnitPrice : RequiredDecimal<UnitPrice>;

/// <summary>Current available stock quantity.</summary>
[AllowZero]
[NonNegative]
public partial class StockQuantity : RequiredInt<StockQuantity>;

/// <summary>Positive stock adjustment quantity.</summary>
[Positive]
public partial class StockAdjustmentQuantity : RequiredInt<StockAdjustmentQuantity>;

/// <summary>Order line item quantity.</summary>
[Range(1, 999)]
public partial class LineItemQuantity : RequiredInt<LineItemQuantity>;
