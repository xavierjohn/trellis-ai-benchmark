namespace OrderManagement.Api.v2026_11_12.Models;

using OrderManagement.Domain;
using Trellis.Primitives;

/// <summary>Request body for creating a customer.</summary>
public sealed record CreateCustomerRequest(
    FirstName FirstName,
    LastName LastName,
    EmailAddress Email,
    PhoneNumber? Phone,
    ShippingAddress ShippingAddress);

/// <summary>Request body for creating a product.</summary>
public sealed record CreateProductRequest(
    ProductName Name,
    Sku Sku,
    UnitPrice UnitPrice);

/// <summary>Request body for adding stock to a product.</summary>
public sealed record AddStockRequest(int Quantity);

/// <summary>A single requested line in a create-order request.</summary>
public sealed record OrderLineRequest(ProductId ProductId, Quantity Quantity);

/// <summary>Request body for creating a draft order.</summary>
public sealed record CreateOrderRequest(
    CustomerId CustomerId,
    IReadOnlyList<OrderLineRequest> Lines);

/// <summary>Request body for adding a line item to a draft order.</summary>
public sealed record AddLineItemRequest(ProductId ProductId, Quantity Quantity);
