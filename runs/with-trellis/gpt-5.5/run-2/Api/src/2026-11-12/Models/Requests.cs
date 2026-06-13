namespace OrderManagement.Api.v2026_11_12.Models;

public sealed record ShippingAddressRequest(string? Street, string? City, string? State, string? PostalCode, string? Country);

public sealed record CreateCustomerRequest(string? FirstName, string? LastName, string? Email, string? PhoneNumber, ShippingAddressRequest? ShippingAddress);

public sealed record CreateProductRequest(string? ProductName, string? Sku, decimal UnitPrice);

public sealed record AddStockRequest(int Quantity);

public sealed record CreateOrderLineItemRequest(Guid ProductId, int Quantity);

public sealed record CreateOrderRequest(Guid CustomerId, IReadOnlyList<CreateOrderLineItemRequest>? LineItems);

public sealed record AddLineItemRequest(Guid ProductId, int Quantity);
