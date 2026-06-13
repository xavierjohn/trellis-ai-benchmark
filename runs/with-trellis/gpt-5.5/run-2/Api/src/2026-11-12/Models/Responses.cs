namespace OrderManagement.Api.v2026_11_12.Models;

using OrderManagement.Domain;

public sealed record ShippingAddressResponse(string Street, string City, string State, string PostalCode, string Country)
{
    public static ShippingAddressResponse From(ShippingAddress address) =>
        new(address.Street.Value, address.City.Value, address.State.Value, address.PostalCode.Value, address.Country.Value);
}

public sealed record CustomerResponse(Guid Id, string FirstName, string LastName, string Email, string? PhoneNumber, ShippingAddressResponse ShippingAddress)
{
    public static CustomerResponse From(Customer customer) =>
        new((Guid)customer.Id, customer.FirstName.Value, customer.LastName.Value, customer.Email.Value,
            customer.PhoneNumber.TryGetValue(out var phone) ? phone.Value : null,
            ShippingAddressResponse.From(customer.ShippingAddress));
}

public sealed record ProductResponse(Guid Id, string ProductName, string Sku, decimal UnitPrice, int StockQuantity)
{
    public static ProductResponse From(Product product) =>
        new((Guid)product.Id, product.ProductName.Value, product.Sku.Value, product.UnitPrice.Value, product.StockQuantity.Value);
}

public sealed record OrderLineItemResponse(Guid Id, Guid ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal)
{
    public static OrderLineItemResponse From(OrderLineItem item) =>
        new((Guid)item.Id, (Guid)item.ProductId, item.ProductName.Value, item.Quantity.Value, item.UnitPrice.Value, item.LineTotal);
}

public sealed record OrderResponse(
    Guid Id,
    Guid CustomerId,
    string CreatedByActorId,
    string Status,
    DateTime? SubmittedAt,
    DateTime? ShippedAt,
    decimal Total,
    IReadOnlyList<OrderLineItemResponse> LineItems)
{
    public static OrderResponse From(Order order) =>
        new(
            (Guid)order.Id,
            (Guid)order.CustomerId,
            order.CreatedByActorId,
            order.Status.Value,
            order.SubmittedAt.TryGetValue(out var submittedAt) ? submittedAt : null,
            order.ShippedAt.TryGetValue(out var shippedAt) ? shippedAt : null,
            order.Total,
            order.LineItems.Select(OrderLineItemResponse.From).ToArray());
}
