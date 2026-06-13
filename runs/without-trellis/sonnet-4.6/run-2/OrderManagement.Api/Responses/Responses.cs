using OrderManagement.Api.Domain;

namespace OrderManagement.Api.Responses;

public record ShippingAddressResponse(
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country);

public record CustomerResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    ShippingAddressResponse ShippingAddress);

public record ProductResponse(
    Guid Id,
    string ProductName,
    string Sku,
    decimal UnitPrice,
    int StockQuantity);

public record LineItemResponse(
    Guid Id,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);

public record OrderResponse(
    Guid Id,
    Guid CustomerId,
    string CreatedByActorId,
    string Status,
    List<LineItemResponse> LineItems,
    decimal OrderTotal,
    DateTime CreatedAt,
    DateTime? SubmittedAt,
    DateTime? ShippedAt);

public static class ResponseMappers
{
    public static CustomerResponse ToResponse(this Customer c) =>
        new(c.Id, c.FirstName, c.LastName, c.Email, c.PhoneNumber,
            new ShippingAddressResponse(
                c.ShippingAddress.Street, c.ShippingAddress.City,
                c.ShippingAddress.State, c.ShippingAddress.PostalCode,
                c.ShippingAddress.Country));

    public static ProductResponse ToResponse(this Product p) =>
        new(p.Id, p.ProductName, p.SKU, p.UnitPrice, p.StockQuantity);

    public static OrderResponse ToResponse(this Order o) =>
        new(o.Id, o.CustomerId, o.CreatedByActorId, o.Status.ToString(),
            o.LineItems.Select(li => new LineItemResponse(
                li.Id, li.ProductId, li.ProductName, li.Quantity, li.UnitPrice)).ToList(),
            o.OrderTotal, o.CreatedAt, o.SubmittedAt, o.ShippedAt);
}
