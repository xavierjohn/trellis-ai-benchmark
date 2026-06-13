namespace OrderManagement.Api.Domain;

public record ShippingAddress(
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country);
