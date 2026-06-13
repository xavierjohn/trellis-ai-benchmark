namespace OrderManagement.Api.Models;

public record ShippingAddressRequest(
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country);

public record CreateCustomerRequest(
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    ShippingAddressRequest ShippingAddress);

public record CreateProductRequest(
    string ProductName,
    string Sku,
    decimal UnitPrice);

public record AddStockRequest(int Quantity);

public record OrderLineItemRequest(Guid ProductId, int Quantity);

public record CreateOrderRequest(
    Guid CustomerId,
    List<OrderLineItemRequest> LineItems);

public record AddLineItemRequest(Guid ProductId, int Quantity);
