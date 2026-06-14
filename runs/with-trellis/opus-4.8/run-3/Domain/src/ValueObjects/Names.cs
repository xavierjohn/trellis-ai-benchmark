namespace OrderManagement.Domain;

/// <summary>Customer first name. 1–100 characters.</summary>
[StringLength(100)]
public partial class FirstName : RequiredString<FirstName>
{
    static partial void ValidateAdditional(string value, string fieldName, ref string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
            errorMessage = "First name cannot be empty or whitespace.";
    }
}

/// <summary>Customer last name. 1–100 characters.</summary>
[StringLength(100)]
public partial class LastName : RequiredString<LastName>
{
    static partial void ValidateAdditional(string value, string fieldName, ref string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
            errorMessage = "Last name cannot be empty or whitespace.";
    }
}

/// <summary>Product name. 1–200 characters.</summary>
[StringLength(200)]
public partial class ProductName : RequiredString<ProductName>
{
    static partial void ValidateAdditional(string value, string fieldName, ref string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
            errorMessage = "Product name cannot be empty or whitespace.";
    }
}
