namespace OrderManagement.Domain;

/// <summary>Unique customer identifier.</summary>
public partial class CustomerId : RequiredGuid<CustomerId>;

/// <summary>Unique product identifier.</summary>
public partial class ProductId : RequiredGuid<ProductId>;

/// <summary>Unique order identifier.</summary>
public partial class OrderId : RequiredGuid<OrderId>;

/// <summary>Unique order line item identifier.</summary>
public partial class LineItemId : RequiredGuid<LineItemId>;
