namespace OrderManagement.Domain;

using System.Text.RegularExpressions;

/// <summary>
/// Stock Keeping Unit — a unique alphanumeric identifier for a product.
/// 3–20 characters, uppercase letters and digits only.
/// </summary>
[StringLength(20, MinimumLength = 3)]
public partial class Sku : RequiredString<Sku>
{
    static partial void ValidateAdditional(string value, string fieldName, ref string? errorMessage)
    {
        if (!string.IsNullOrEmpty(value) && !Regex.IsMatch(value, "^[A-Z0-9]+$"))
            errorMessage = "SKU must contain only uppercase letters and digits.";
    }
}
