namespace OrderManagement.Api.v2026_11_12.Models;

/// <summary>A shipping address on the request wire.</summary>
/// <param name="Street">Street.</param>
/// <param name="City">City.</param>
/// <param name="State">State.</param>
/// <param name="PostalCode">Postal code.</param>
/// <param name="Country">Country.</param>
public sealed record ShippingAddressRequest(
    string? Street,
    string? City,
    string? State,
    string? PostalCode,
    string? Country);

/// <summary>Request body for creating a customer.</summary>
/// <param name="FirstName">First name.</param>
/// <param name="LastName">Last name.</param>
/// <param name="Email">Email address.</param>
/// <param name="PhoneNumber">Optional phone number.</param>
/// <param name="ShippingAddress">Shipping address.</param>
public sealed record CreateCustomerRequest(
    string? FirstName,
    string? LastName,
    string? Email,
    string? PhoneNumber,
    ShippingAddressRequest? ShippingAddress);

/// <summary>Request body for creating a product.</summary>
/// <param name="Name">Product name.</param>
/// <param name="Sku">Stock keeping unit.</param>
/// <param name="UnitPrice">Unit price (USD); must be greater than zero.</param>
public sealed record CreateProductRequest(string? Name, string? Sku, decimal UnitPrice);

/// <summary>Request body for adding stock to a product.</summary>
/// <param name="Quantity">Quantity to add; must be positive.</param>
public sealed record AddStockRequest(int Quantity);

/// <summary>A single requested order line.</summary>
/// <param name="ProductId">Product id.</param>
/// <param name="Quantity">Quantity (1–999).</param>
public sealed record CreateOrderLineDto(Guid ProductId, int Quantity);

/// <summary>Request body for creating a draft order.</summary>
/// <param name="CustomerId">Customer id.</param>
/// <param name="Lines">Requested order lines (at least one, no duplicate products).</param>
public sealed record CreateOrderRequest(Guid CustomerId, IReadOnlyList<CreateOrderLineDto>? Lines);

/// <summary>Request body for adding a line item to a draft order.</summary>
/// <param name="ProductId">Product id.</param>
/// <param name="Quantity">Quantity (1–999).</param>
public sealed record AddLineItemRequest(Guid ProductId, int Quantity);
