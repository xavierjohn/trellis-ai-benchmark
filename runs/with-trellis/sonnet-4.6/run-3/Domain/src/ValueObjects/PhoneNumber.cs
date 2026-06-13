namespace OrderManagement.Domain;

[StringLength(20, MinimumLength = 7)]
public partial class PhoneNumber : RequiredString<PhoneNumber>
{
    static partial void ValidateAdditional(string value, string fieldName, ref string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errorMessage = "Phone number cannot be empty or whitespace.";
            return;
        }

        if (!value.Any(char.IsDigit))
            errorMessage = "Phone number must contain at least one digit.";
    }
}
