using OrderManagement.Api.Contracts;
using OrderManagement.Api.Domain.Customers;
using OrderManagement.Api.Domain.Orders;
using OrderManagement.Api.Domain.Products;

namespace OrderManagement.Api.Application;

/// <summary>Maps domain entities to response DTOs.</summary>
public static class Mappers
{
    public static CustomerResponse ToResponse(this Customer c) => new(
        c.Id,
        c.FirstName,
        c.LastName,
        c.Email,
        c.PhoneNumber,
        new ShippingAddressDto(
            c.ShippingAddress.Street,
            c.ShippingAddress.City,
            c.ShippingAddress.State,
            c.ShippingAddress.PostalCode,
            c.ShippingAddress.Country));

    public static ProductResponse ToResponse(this Product p) => new(
        p.Id, p.ProductName, p.Sku, p.UnitPrice, p.StockQuantity);

    public static OrderResponse ToResponse(this Order o) => new(
        o.Id,
        o.CustomerId,
        o.CreatedByActorId,
        o.Status.ToString(),
        o.CreatedAt,
        o.SubmittedAt,
        o.ShippedAt,
        o.OrderTotal,
        o.LineItems.Select(li => new LineItemResponse(
            li.Id, li.ProductId, li.ProductName, li.Quantity, li.UnitPrice, li.LineTotal)).ToList());
}
