namespace OrderManagement.Domain;

using System.Text.RegularExpressions;

/// <summary>
/// Stock Keeping Unit — 3–20 characters, uppercase letters and digits only.
/// </summary>
[StringLength(20, MinimumLength = 3)]
public sealed partial class Sku : RequiredString<Sku>
{
    static partial void ValidateAdditional(string value, string fieldName, ref string? errorMessage)
    {
        if (!SkuPattern().IsMatch(value))
            errorMessage = "SKU must contain only uppercase letters and digits.";
    }

    [GeneratedRegex(@"^[A-Z0-9]+$")]
    private static partial Regex SkuPattern();
}
