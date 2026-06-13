using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Products;

namespace OrderManagement.Application.Abstractions;

// ---- Response DTOs ----

public sealed record AddressDto(string Street, string City, string State, string PostalCode, string Country);

public sealed record CustomerDto(
    Guid Id, string FirstName, string LastName, string Email, string? PhoneNumber, AddressDto ShippingAddress);

public sealed record ProductDto(Guid Id, string ProductName, string Sku, decimal UnitPrice, int StockQuantity);

public sealed record LineItemDto(
    Guid Id, Guid ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal);

public sealed record OrderDto(
    Guid Id,
    Guid CustomerId,
    string CreatedByActorId,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? ShippedAt,
    decimal OrderTotal,
    IReadOnlyList<LineItemDto> LineItems);

// ---- Request models ----

public sealed record AddressRequest(string? Street, string? City, string? State, string? PostalCode, string? Country);

public sealed record CreateCustomerRequest(
    string? FirstName, string? LastName, string? Email, string? PhoneNumber, AddressRequest? ShippingAddress);

public sealed record CreateProductRequest(string? ProductName, string? Sku, decimal UnitPrice);

public sealed record AddStockRequest(int Quantity);

public sealed record OrderLineRequest(Guid ProductId, int Quantity);

public sealed record CreateOrderRequest(Guid CustomerId, IReadOnlyList<OrderLineRequest>? LineItems);

public sealed record AddLineItemRequest(Guid ProductId, int Quantity);

public static class DtoMapper
{
    public static AddressDto ToDto(this ShippingAddress a) =>
        new(a.Street, a.City, a.State, a.PostalCode, a.Country);

    public static CustomerDto ToDto(this Customer c) =>
        new(c.Id, c.FirstName, c.LastName, c.Email, c.PhoneNumber, c.ShippingAddress.ToDto());

    public static ProductDto ToDto(this Product p) =>
        new(p.Id, p.ProductName, p.Sku, p.UnitPrice, p.StockQuantity);

    public static LineItemDto ToDto(this LineItem li) =>
        new(li.Id, li.ProductId, li.ProductName, li.Quantity, li.UnitPrice, li.LineTotal);

    public static OrderDto ToDto(this Order o) =>
        new(o.Id, o.CustomerId, o.CreatedByActorId, o.Status.ToString(), o.CreatedAt, o.SubmittedAt,
            o.ShippedAt, o.OrderTotal, o.LineItems.Select(li => li.ToDto()).ToList());
}
