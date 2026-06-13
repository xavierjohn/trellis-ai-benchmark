namespace OrderManagement.Domain;

public partial class CustomerId : RequiredGuid<CustomerId>
{
    static partial void ValidateAdditional(Guid value, string fieldName, ref string? errorMessage)
    {
        if (value == Guid.Empty)
            errorMessage = "Customer Id cannot be empty.";
    }
}
