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
    /// <summary>Project a customer.</summary>
    public static CustomerResponse From(Customer customer)
    {
        customer.PhoneNumber.TryGetValue(out var phoneNumber);
        return new CustomerResponse(
            (Guid)customer.Id,
            customer.FirstName.Value,
            customer.LastName.Value,
            customer.Email.Value,
            phoneNumber?.Value,
            ShippingAddressResponse.From(customer.ShippingAddress));
    }
}

/// <summary>Shipping address response.</summary>
public sealed record ShippingAddressResponse(string Street, string City, string State, string PostalCode, string Country)
{
    /// <summary>Project an address.</summary>
    public static ShippingAddressResponse From(ShippingAddress address) =>
        new(address.Street, address.City, address.State, address.PostalCode, address.Country);
}

/// <summary>Product response.</summary>
public sealed record ProductResponse(Guid Id, string ProductName, string Sku, decimal UnitPrice, int StockQuantity)
{
    /// <summary>Project a product.</summary>
    public static ProductResponse From(Product product) =>
        new((Guid)product.Id, product.ProductName.Value, product.Sku.Value, product.UnitPrice.Value, product.StockQuantity.Value);
}

/// <summary>Order response.</summary>
public sealed record OrderResponse(
    Guid Id,
    Guid CustomerId,
    string CreatedByActorId,
    string Status,
    DateTimeOffset CreatedAt,
    DateTime? SubmittedAt,
    DateTime? ShippedAt,
    decimal Total,
    IReadOnlyList<LineItemResponse> LineItems)
{
    /// <summary>Project an order.</summary>
    public static OrderResponse From(Order order)
    {
        order.SubmittedAt.TryGetValue(out var submittedAt);
        order.ShippedAt.TryGetValue(out var shippedAt);
        return new OrderResponse(
            (Guid)order.Id,
            (Guid)order.CustomerId,
            order.CreatedByActorId.Value,
            order.Status.Value,
            order.CreatedAt,
            submittedAt == default ? null : submittedAt,
            shippedAt == default ? null : shippedAt,
            order.Total,
            order.LineItems.Select(LineItemResponse.From).ToArray());
    }
}

/// <summary>Line item response.</summary>
public sealed record LineItemResponse(Guid Id, Guid ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal Total)
{
    /// <summary>Project a line item.</summary>
    public static LineItemResponse From(LineItem item) =>
        new((Guid)item.Id, (Guid)item.ProductId, item.ProductName.Value, item.Quantity.Value, item.UnitPrice.Value, item.Total);
}
