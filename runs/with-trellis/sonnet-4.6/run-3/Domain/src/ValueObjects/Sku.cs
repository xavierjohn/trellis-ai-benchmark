namespace OrderManagement.Domain;

[StringLength(20, MinimumLength = 3)]
public partial class Sku : RequiredString<Sku>
{
    static partial void ValidateAdditional(string value, string fieldName, ref string? errorMessage)
    {
        if (value.Length is < 3 or > 20)
        {
            errorMessage = "Sku must be between 3 and 20 characters.";
            return;
        }

        if (value.Any(c => !char.IsAsciiLetterOrDigit(c) || char.IsLower(c)))
            errorMessage = "Sku must contain only uppercase letters and digits.";
    }
}
