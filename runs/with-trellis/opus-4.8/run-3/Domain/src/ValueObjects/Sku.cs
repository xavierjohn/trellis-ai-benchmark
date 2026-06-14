namespace OrderManagement.Domain;

using System.Text.RegularExpressions;

/// <summary>
/// Stock Keeping Unit. 3–20 characters, uppercase letters and digits only, unique per product.
/// </summary>
[StringLength(20, MinimumLength = 3)]
public partial class Sku : RequiredString<Sku>
{
    static partial void ValidateAdditional(string value, string fieldName, ref string? errorMessage)
    {
        if (!SkuPattern().IsMatch(value))
            errorMessage = "SKU must be 3–20 characters of uppercase letters and digits only.";
    }

    [GeneratedRegex("^[A-Z0-9]{3,20}$")]
    private static partial Regex SkuPattern();
}
