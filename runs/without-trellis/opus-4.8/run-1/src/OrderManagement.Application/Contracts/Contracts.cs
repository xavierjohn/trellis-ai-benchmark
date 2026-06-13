using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Products;

namespace OrderManagement.Application.Contracts;

// ----- Requests -----

public sealed record ShippingAddressDto(
    string? Street, string? City, string? State, string? PostalCode, string? Country);

public sealed record CreateCustomerRequest(
    string? FirstName, string? LastName, string? Email, string? PhoneNumber, ShippingAddressDto? ShippingAddress);

public sealed record CreateProductRequest(string? ProductName, string? Sku, decimal UnitPrice);

public sealed record AddStockRequest(int Quantity);

public sealed record OrderLineRequest(Guid ProductId, int Quantity);

public sealed record CreateOrderRequest(Guid CustomerId, IReadOnlyList<OrderLineRequest>? Lines);

public sealed record AddLineItemRequest(Guid ProductId, int Quantity);

// ----- Responses -----

public sealed record CustomerResponse(
    Guid Id, string FirstName, string LastName, string Email, string? PhoneNumber, ShippingAddressDto ShippingAddress)
{
    public static CustomerResponse From(Customer c) => new(
        c.Id, c.FirstName, c.LastName, c.Email.Value, c.PhoneNumber,
        new ShippingAddressDto(
            c.ShippingAddress.Street, c.ShippingAddress.City, c.ShippingAddress.State,
            c.ShippingAddress.PostalCode, c.ShippingAddress.Country));
}

public sealed record ProductResponse(Guid Id, string ProductName, string Sku, decimal UnitPrice, int StockQuantity)
{
    public static ProductResponse From(Product p) => new(p.Id, p.ProductName, p.Sku.Value, p.UnitPrice, p.StockQuantity);
}

public sealed record LineItemResponse(
    Guid LineItemId, Guid ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal)
{
    public static LineItemResponse From(LineItem li) =>
        new(li.Id, li.ProductId, li.ProductName, li.Quantity, li.UnitPrice, li.LineTotal);
}

public sealed record OrderResponse(
    Guid Id,
    Guid CustomerId,
    string CreatedByActorId,
    string Status,
    decimal OrderTotal,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? ShippedAt,
    IReadOnlyList<LineItemResponse> LineItems)
{
    public static OrderResponse From(Order o) => new(
        o.Id, o.CustomerId, o.CreatedByActorId, o.Status.ToString(), o.OrderTotal,
        o.CreatedAt, o.SubmittedAt, o.ShippedAt,
        o.LineItems.Select(LineItemResponse.From).ToList());
}
