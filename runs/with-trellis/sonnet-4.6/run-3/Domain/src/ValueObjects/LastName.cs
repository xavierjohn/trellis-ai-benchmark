namespace OrderManagement.Domain;

[StringLength(100, MinimumLength = 1)]
public partial class LastName : RequiredString<LastName>
{
    static partial void ValidateAdditional(string value, string fieldName, ref string? errorMessage)
    {
        if (value.Length > 100)
            errorMessage = "Last name cannot exceed 100 characters.";
    }
}
