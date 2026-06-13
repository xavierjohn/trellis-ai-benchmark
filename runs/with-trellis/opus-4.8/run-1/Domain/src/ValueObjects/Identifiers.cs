namespace OrderManagement.Domain;

/// <summary>Unique identifier for a customer.</summary>
public partial class CustomerId : RequiredGuid<CustomerId>
{
    static partial void ValidateAdditional(Guid value, string fieldName, ref string? errorMessage)
    {
        if (value == Guid.Empty)
            errorMessage = "Customer Id cannot be empty.";
    }
}

/// <summary>Unique identifier for a product.</summary>
public partial class ProductId : RequiredGuid<ProductId>
{
    static partial void ValidateAdditional(Guid value, string fieldName, ref string? errorMessage)
    {
        if (value == Guid.Empty)
            errorMessage = "Product Id cannot be empty.";
    }
}

/// <summary>Unique identifier for an order.</summary>
public partial class OrderId : RequiredGuid<OrderId>
{
    static partial void ValidateAdditional(Guid value, string fieldName, ref string? errorMessage)
    {
        if (value == Guid.Empty)
            errorMessage = "Order Id cannot be empty.";
    }
}

/// <summary>Unique identifier for a line item.</summary>
public partial class LineItemId : RequiredGuid<LineItemId>
{
    static partial void ValidateAdditional(Guid value, string fieldName, ref string? errorMessage)
    {
        if (value == Guid.Empty)
            errorMessage = "Line Item Id cannot be empty.";
    }
}
