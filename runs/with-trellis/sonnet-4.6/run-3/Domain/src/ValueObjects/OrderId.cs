namespace OrderManagement.Domain;

public partial class OrderId : RequiredGuid<OrderId>
{
    static partial void ValidateAdditional(Guid value, string fieldName, ref string? errorMessage)
    {
        if (value == Guid.Empty)
            errorMessage = "Order Id cannot be empty.";
    }
}
