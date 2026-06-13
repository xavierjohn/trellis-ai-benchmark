namespace OrderManagement.Api;

public sealed record AddressDto(string Street, string City, string State, string PostalCode, string Country);
public sealed record CreateCustomerRequest(string FirstName, string LastName, string Email, string? PhoneNumber, AddressDto ShippingAddress);
public sealed record CustomerResponse(Guid Id, string FirstName, string LastName, string Email, string? PhoneNumber, AddressDto ShippingAddress);

public sealed record CreateProductRequest(string ProductName, string Sku, decimal UnitPrice);
public sealed record AddStockRequest(int Quantity);
public sealed record ProductResponse(Guid Id, string ProductName, string Sku, decimal UnitPrice, int StockQuantity);

public sealed record CreateOrderLineItemRequest(Guid ProductId, int Quantity);
public sealed record CreateOrderRequest(Guid CustomerId, List<CreateOrderLineItemRequest> LineItems);
public sealed record AddLineItemRequest(Guid ProductId, int Quantity);
public sealed record OrderLineItemResponse(Guid Id, Guid ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal);
public sealed record OrderResponse(
    Guid Id,
    Guid CustomerId,
    string CreatedByActorId,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? ShippedAt,
    decimal Total,
    IReadOnlyCollection<OrderLineItemResponse> LineItems);

public static class ResponseMapping
{
    public static CustomerResponse ToResponse(this Customer customer) =>
        new(customer.Id, customer.FirstName, customer.LastName, customer.Email, customer.PhoneNumber,
            new AddressDto(customer.ShippingAddress.Street, customer.ShippingAddress.City, customer.ShippingAddress.State, customer.ShippingAddress.PostalCode, customer.ShippingAddress.Country));

    public static ProductResponse ToResponse(this Product product) =>
        new(product.Id, product.ProductName, product.Sku, product.UnitPrice, product.StockQuantity);

    public static OrderResponse ToResponse(this Order order) =>
        new(order.Id, order.CustomerId, order.CreatedByActorId, order.Status.ToString(), order.CreatedAt, order.SubmittedAt, order.ShippedAt, order.Total,
            order.LineItems.Select(i => new OrderLineItemResponse(i.Id, i.ProductId, i.ProductName, i.Quantity, i.UnitPrice, i.UnitPrice * i.Quantity)).ToArray());
}
