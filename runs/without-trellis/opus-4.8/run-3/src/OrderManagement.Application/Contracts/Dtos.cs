namespace OrderManagement.Application.Contracts;

// ----- Requests -----

public sealed record ShippingAddressDto(string Street, string City, string State, string PostalCode, string Country);

public sealed record CreateCustomerRequest(
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    ShippingAddressDto ShippingAddress);

public sealed record CreateProductRequest(string ProductName, string Sku, decimal UnitPrice, int InitialStock = 0);

public sealed record AddStockRequest(int Quantity);

public sealed record OrderLineRequest(Guid ProductId, int Quantity);

public sealed record CreateOrderRequest(Guid CustomerId, IReadOnlyList<OrderLineRequest> Lines);

public sealed record AddLineItemRequest(Guid ProductId, int Quantity);

// ----- Responses -----

public sealed record CustomerResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    ShippingAddressDto ShippingAddress);

public sealed record ProductResponse(Guid Id, string ProductName, string Sku, decimal UnitPrice, int StockQuantity);

public sealed record LineItemResponse(Guid Id, Guid ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal);

public sealed record OrderResponse(
    Guid Id,
    Guid CustomerId,
    string CreatedByActorId,
    string Status,
    decimal OrderTotal,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? ShippedAt,
    IReadOnlyList<LineItemResponse> LineItems);
