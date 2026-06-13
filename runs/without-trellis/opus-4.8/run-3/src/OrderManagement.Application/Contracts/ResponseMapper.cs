using OrderManagement.Application.Contracts;
using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Products;

namespace OrderManagement.Application.Contracts;

public static class ResponseMapper
{
    public static CustomerResponse ToResponse(this Customer c) => new(
        c.Id, c.FirstName, c.LastName, c.Email, c.PhoneNumber,
        new ShippingAddressDto(
            c.ShippingAddress.Street, c.ShippingAddress.City, c.ShippingAddress.State,
            c.ShippingAddress.PostalCode, c.ShippingAddress.Country));

    public static ProductResponse ToResponse(this Product p) => new(
        p.Id, p.ProductName, p.Sku, p.UnitPrice, p.StockQuantity);

    public static OrderResponse ToResponse(this Order o) => new(
        o.Id, o.CustomerId, o.CreatedByActorId, o.Status.ToString(), o.OrderTotal,
        o.CreatedAt, o.SubmittedAt, o.ShippedAt,
        o.LineItems.Select(li => new LineItemResponse(
            li.Id, li.ProductId, li.ProductName, li.Quantity, li.UnitPrice, li.LineTotal)).ToList());
}
