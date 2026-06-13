using OrderManagement.Domain.Aggregates;
using OrderManagement.Domain.Enums;

namespace OrderManagement.Application.DTOs;

public record ShippingAddressDto(
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country);

public record CustomerDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    ShippingAddressDto ShippingAddress)
{
    public static CustomerDto FromCustomer(Customer c) => new(
        c.Id, c.FirstName, c.LastName, c.Email, c.PhoneNumber,
        new ShippingAddressDto(
            c.ShippingAddress.Street, c.ShippingAddress.City,
            c.ShippingAddress.State, c.ShippingAddress.PostalCode,
            c.ShippingAddress.Country));
}

public record ProductDto(
    Guid Id,
    string ProductName,
    string SKU,
    decimal UnitPrice,
    int StockQuantity)
{
    public static ProductDto FromProduct(Product p) => new(
        p.Id, p.ProductName, p.SKU, p.UnitPrice, p.StockQuantity);
}

public record LineItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public record OrderDto(
    Guid Id,
    Guid CustomerId,
    string CreatedByActorId,
    OrderStatus Status,
    decimal OrderTotal,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? ShippedAt,
    IReadOnlyList<LineItemDto> LineItems)
{
    public static OrderDto FromOrder(Order o) => new(
        o.Id,
        o.CustomerId,
        o.CreatedByActorId,
        o.Status,
        o.OrderTotal,
        o.CreatedAt,
        o.SubmittedAt,
        o.ShippedAt,
        o.LineItems.Select(li => new LineItemDto(
            li.Id, li.ProductId, li.ProductName, li.Quantity, li.UnitPrice, li.LineTotal)
        ).ToList());
}
