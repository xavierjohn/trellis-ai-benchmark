namespace OrderManagement.Domain;

/// <summary>
/// Unique identifier for a customer.
/// </summary>
public partial class CustomerId : RequiredGuid<CustomerId>
{
}

/// <summary>
/// Unique identifier for a product.
/// </summary>
public partial class ProductId : RequiredGuid<ProductId>
{
}

/// <summary>
/// Unique identifier for an order.
/// </summary>
public partial class OrderId : RequiredGuid<OrderId>
{
}

/// <summary>
/// Unique identifier for an order line item.
/// </summary>
public partial class LineItemId : RequiredGuid<LineItemId>
{
}
