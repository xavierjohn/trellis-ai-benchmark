namespace OrderManagement.Domain;

public partial class Email : RequiredString<Email>
{
    static partial void ValidateAdditional(string value, string fieldName, ref string? errorMessage)
    {
        if (!value.Contains('@') || !value.Contains('.'))
            errorMessage = "Email must contain '@' and '.'.";
    }
}
