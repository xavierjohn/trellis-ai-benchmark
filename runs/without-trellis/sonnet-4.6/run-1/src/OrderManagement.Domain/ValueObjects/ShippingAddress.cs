namespace OrderManagement.Domain.ValueObjects;

public record ShippingAddress(
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country);
