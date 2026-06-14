namespace OrderManagement.Api.Contracts;

// ----- Requests -----

public sealed record ShippingAddressDto(
    string? Street,
    string? City,
    string? State,
    string? PostalCode,
    string? Country);

public sealed record CreateCustomerRequest(
    string? FirstName,
    string? LastName,
    string? Email,
    string? PhoneNumber,
    ShippingAddressDto? ShippingAddress);

public sealed record CreateProductRequest(
    string? ProductName,
    string? Sku,
    decimal UnitPrice);

public sealed record AddStockRequest(int Quantity);

public sealed record CreateOrderLineItemDto(Guid ProductId, int Quantity);

public sealed record CreateOrderRequest(
    Guid CustomerId,
    List<CreateOrderLineItemDto>? Items);

public sealed record AddLineItemRequest(Guid ProductId, int Quantity);

// ----- Responses -----

public sealed record CustomerResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    ShippingAddressDto ShippingAddress);

public sealed record ProductResponse(
    Guid Id,
    string ProductName,
    string Sku,
    decimal UnitPrice,
    int StockQuantity);

public sealed record LineItemResponse(
    Guid LineItemId,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public sealed record OrderResponse(
    Guid Id,
    Guid CustomerId,
    string CreatedByActorId,
    string Status,
    DateTime CreatedAt,
    DateTime? SubmittedAt,
    DateTime? ShippedAt,
    decimal OrderTotal,
    IReadOnlyList<LineItemResponse> LineItems);
