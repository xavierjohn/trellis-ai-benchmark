namespace OrderManagement.Api.Models;

public record CreateCustomerRequest(
    string? FirstName,
    string? LastName,
    string? Email,
    string? PhoneNumber,
    string? Street,
    string? City,
    string? State,
    string? PostalCode,
    string? Country);

public record CreateProductRequest(
    string? ProductName,
    string? SKU,
    decimal? UnitPrice);

public record AddStockRequest(int Quantity);

public record CreateOrderLineItemInput(Guid ProductId, int Quantity);

public record CreateOrderRequest(
    Guid? CustomerId,
    IReadOnlyList<CreateOrderLineItemInput>? LineItems);

public record AddLineItemRequest(Guid ProductId, int Quantity);
