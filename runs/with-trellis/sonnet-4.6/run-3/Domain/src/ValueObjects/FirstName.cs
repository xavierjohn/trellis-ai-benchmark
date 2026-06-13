namespace OrderManagement.Domain;

[StringLength(100, MinimumLength = 1)]
public partial class FirstName : RequiredString<FirstName>
{
    static partial void ValidateAdditional(string value, string fieldName, ref string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
            errorMessage = "First name cannot be empty or whitespace.";
        else if (value.Length > 100)
            errorMessage = "First name cannot exceed 100 characters.";
    }
}
