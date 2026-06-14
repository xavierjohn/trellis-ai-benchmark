namespace Application.Customers;

public record CreateCustomerCommand(
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country);
