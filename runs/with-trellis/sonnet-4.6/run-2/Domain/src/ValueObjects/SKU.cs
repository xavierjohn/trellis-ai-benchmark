namespace OrderManagement.Domain;

/// <summary>Stock-Keeping Unit — uppercase alphanumeric, 3–20 characters.</summary>
[StringLength(20, MinimumLength = 3)]
public partial class SKU : RequiredString<SKU>
{
    static partial void ValidateAdditional(string value, string fieldName, ref string? errorMessage)
    {
        foreach (var ch in value)
        {
            if (!char.IsAsciiLetterUpper(ch) && !char.IsAsciiDigit(ch))
            {
                errorMessage = $"{fieldName} must contain only uppercase letters and digits.";
                return;
            }
        }
    }
}
