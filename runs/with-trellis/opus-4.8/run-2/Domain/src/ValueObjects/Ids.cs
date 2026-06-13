namespace OrderManagement.Domain;

/// <summary>Unique identifier for a customer.</summary>
public sealed partial class CustomerId : RequiredGuid<CustomerId>;

/// <summary>Unique identifier for a product.</summary>
public sealed partial class ProductId : RequiredGuid<ProductId>;

/// <summary>Unique identifier for an order.</summary>
public sealed partial class OrderId : RequiredGuid<OrderId>;

/// <summary>Unique identifier for an order line item.</summary>
public sealed partial class LineItemId : RequiredGuid<LineItemId>;
