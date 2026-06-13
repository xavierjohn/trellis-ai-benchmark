namespace OrderManagement.Domain;

using System.Text.RegularExpressions;

/// <summary>
/// Product stock keeping unit.
/// </summary>
[StringLength(20, MinimumLength = 3)]
public partial class Sku : RequiredString<Sku>
{
    static partial void ValidateAdditional(string value, string fieldName, ref string? errorMessage)
    {
        if (!Regex.IsMatch(value, "^[A-Z0-9]+$"))
            errorMessage = "SKU must contain only uppercase letters and digits.";
    }
}
