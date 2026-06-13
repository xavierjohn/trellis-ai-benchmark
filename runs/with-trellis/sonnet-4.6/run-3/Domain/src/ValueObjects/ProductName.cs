namespace OrderManagement.Domain;

[StringLength(200, MinimumLength = 1)]
public partial class ProductName : RequiredString<ProductName>
{
    static partial void ValidateAdditional(string value, string fieldName, ref string? errorMessage)
    {
        if (value.Length > 200)
            errorMessage = "Product name cannot exceed 200 characters.";
    }
}
