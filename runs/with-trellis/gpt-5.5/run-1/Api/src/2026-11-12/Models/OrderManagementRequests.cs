namespace OrderManagement.Api.v2026_11_12.Models;

/// <summary>Create customer request.</summary>
public sealed record CreateCustomerRequest(
    string? FirstName,
    string? LastName,
    string? Email,
    string? PhoneNumber,
    ShippingAddressRequest? ShippingAddress);

/// <summary>Shipping address request.</summary>
public sealed record ShippingAddressRequest(string? Street, string? City, string? State, string? PostalCode, string? Country);

/// <summary>Create product request.</summary>
public sealed record CreateProductRequest(string? ProductName, string? Sku, decimal UnitPrice);

/// <summary>Add stock request.</summary>
public sealed record AddStockRequest(int Quantity);

/// <summary>Create order request.</summary>
public sealed record CreateOrderRequest(Guid CustomerId, IReadOnlyList<OrderLineItemRequest> LineItems);

/// <summary>Order line item request.</summary>
public sealed record OrderLineItemRequest(Guid ProductId, int Quantity);
