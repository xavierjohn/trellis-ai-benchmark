namespace OrderManagement.Api.v2026_11_12.Models;

using OrderManagement.Domain;

/// <summary>Shipping address request.</summary>
public sealed record ShippingAddressRequest(string? Street, string? City, string? State, string? PostalCode, string? Country);

/// <summary>Create customer request.</summary>
public sealed record CreateCustomerRequest(string? FirstName, string? LastName, string? Email, string? PhoneNumber, ShippingAddressRequest? ShippingAddress);

/// <summary>Create product request.</summary>
public sealed record CreateProductRequest(ProductName Name, Sku Sku, UnitPrice UnitPrice);

/// <summary>Add stock request.</summary>
public sealed record AddStockRequest(StockAdjustmentQuantity Quantity);

/// <summary>Order line item request.</summary>
public sealed record OrderLineItemRequest(ProductId ProductId, OrderQuantity Quantity);

/// <summary>Create order request.</summary>
public sealed record CreateOrderRequest(CustomerId CustomerId, IReadOnlyList<OrderLineItemRequest> LineItems);

/// <summary>Add line item request.</summary>
public sealed record AddLineItemRequest(ProductId ProductId, OrderQuantity Quantity);
