namespace OrderManagement.Domain;

using System.Text.RegularExpressions;

/// <summary>Stock keeping unit. Uppercase letters and digits, 3-20 characters.</summary>
[StringLength(20, MinimumLength = 3)]
public sealed partial class Sku : RequiredString<Sku>
{
    static partial void ValidateAdditional(string value, string fieldName, ref string? errorMessage)
    {
        if (!Regex.IsMatch(value, "^[A-Z0-9]+$", RegexOptions.CultureInvariant))
            errorMessage = "SKU must contain uppercase letters and digits only.";
    }
}
