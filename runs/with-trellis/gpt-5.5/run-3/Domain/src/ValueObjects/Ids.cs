namespace OrderManagement.Domain;

/// <summary>Customer aggregate identifier.</summary>
public partial class CustomerId : RequiredGuid<CustomerId>;

/// <summary>Product aggregate identifier.</summary>
public partial class ProductId : RequiredGuid<ProductId>;

/// <summary>Order aggregate identifier.</summary>
public partial class OrderId : RequiredGuid<OrderId>;

/// <summary>Line item entity identifier.</summary>
public partial class LineItemId : RequiredGuid<LineItemId>;
