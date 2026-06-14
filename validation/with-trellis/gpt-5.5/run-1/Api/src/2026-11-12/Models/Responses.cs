namespace OrderManagement.Api.v2026_11_12.Models;

using OrderManagement.Domain;

/// <summary>Shipping address response.</summary>
public sealed record ShippingAddressResponse(string Street, string City, string State, string PostalCode, string Country)
{
    /// <summary>Maps from domain.</summary>
    public static ShippingAddressResponse From(ShippingAddress address) =>
        new(address.Street, address.City, address.State, address.PostalCode, address.Country);
}

/// <summary>Customer response.</summary>
public sealed record CustomerResponse(Guid Id, string FirstName, string LastName, string Email, string? PhoneNumber, ShippingAddressResponse ShippingAddress)
{
    /// <summary>Maps from domain.</summary>
    public static CustomerResponse From(Customer customer)
    {
        customer.PhoneNumber.TryGetValue(out var phone);
        return new((Guid)customer.Id, customer.FirstName.Value, customer.LastName.Value, customer.Email.Value, phone?.Value, ShippingAddressResponse.From(customer.ShippingAddress));
    }
}

/// <summary>Product response.</summary>
public sealed record ProductResponse(Guid Id, string Name, string Sku, decimal UnitPrice, int StockQuantity)
{
    /// <summary>Maps from domain.</summary>
    public static ProductResponse From(Product product) =>
        new((Guid)product.Id, product.Name.Value, product.Sku.Value, product.UnitPrice.Value, product.StockQuantity.Value);
}

/// <summary>Order line item response.</summary>
public sealed record OrderLineItemResponse(Guid Id, Guid ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal Total)
{
    /// <summary>Maps from domain.</summary>
    public static OrderLineItemResponse From(OrderLineItem item) =>
        new((Guid)item.Id, (Guid)item.ProductId, item.ProductName.Value, item.Quantity.Value, item.UnitPrice.Value, item.Total);
}

/// <summary>Order response.</summary>
public sealed record OrderResponse(
    Guid Id,
    Guid CustomerId,
    string CreatedByActorId,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? ShippedAt,
    decimal Total,
    IReadOnlyList<OrderLineItemResponse> LineItems)
{
    /// <summary>Maps from domain.</summary>
    public static OrderResponse From(Order order)
    {
        order.SubmittedAt.TryGetValue(out var submittedAt);
        order.ShippedAt.TryGetValue(out var shippedAt);
        return new(
            (Guid)order.Id,
            (Guid)order.CustomerId,
            order.CreatedByActorId.Value,
            order.Status.Value,
            order.CreatedAt,
            submittedAt,
            shippedAt,
            order.Total,
            order.LineItems.Select(OrderLineItemResponse.From).ToArray());
    }
}
