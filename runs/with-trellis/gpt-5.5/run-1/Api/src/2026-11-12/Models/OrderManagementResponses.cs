namespace OrderManagement.Api.v2026_11_12.Models;

using OrderManagement.Domain;

/// <summary>Customer response.</summary>
public sealed record CustomerResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    ShippingAddressResponse ShippingAddress)
{
    /// <summary>Map from domain.</summary>
    public static CustomerResponse From(Customer customer) => new(
        customer.Id,
        customer.FirstName,
        customer.LastName,
        customer.Email,
        customer.PhoneNumber,
        new ShippingAddressResponse(customer.Street, customer.City, customer.State, customer.PostalCode, customer.Country));
}

/// <summary>Shipping address response.</summary>
public sealed record ShippingAddressResponse(string Street, string City, string State, string PostalCode, string Country);

/// <summary>Product response.</summary>
public sealed record ProductResponse(Guid Id, string ProductName, string Sku, decimal UnitPrice, int StockQuantity)
{
    /// <summary>Map from domain.</summary>
    public static ProductResponse From(Product product) => new(product.Id, product.ProductName, product.Sku, product.UnitPrice, product.StockQuantity);
}

/// <summary>Order response.</summary>
public sealed record OrderResponse(
    Guid Id,
    Guid CustomerId,
    string CreatedByActorId,
    string Status,
    DateTime CreatedAt,
    DateTime? SubmittedAt,
    DateTime? ShippedAt,
    decimal Total,
    IReadOnlyList<OrderLineItemResponse> LineItems)
{
    /// <summary>Map from domain.</summary>
    public static OrderResponse From(Order order) => new(
        order.Id,
        order.CustomerId,
        order.CreatedByActorId,
        order.Status.ToString(),
        order.CreatedAt,
        order.SubmittedAt,
        order.ShippedAt,
        order.Total,
        order.LineItems.Select(OrderLineItemResponse.From).ToList());
}

/// <summary>Order line item response.</summary>
public sealed record OrderLineItemResponse(Guid Id, Guid ProductId, string ProductName, int Quantity, decimal UnitPrice)
{
    /// <summary>Map from domain.</summary>
    public static OrderLineItemResponse From(OrderLineItem lineItem) =>
        new(lineItem.Id, lineItem.ProductId, lineItem.ProductName, lineItem.Quantity, lineItem.UnitPrice);
}
