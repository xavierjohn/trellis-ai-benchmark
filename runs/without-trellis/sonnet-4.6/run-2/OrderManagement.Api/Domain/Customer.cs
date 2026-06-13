namespace OrderManagement.Api.Domain;

public class Customer
{
    public Guid Id { get; private set; }
    public string FirstName { get; private set; } = "";
    public string LastName { get; private set; } = "";
    public string Email { get; private set; } = "";
    public string? PhoneNumber { get; private set; }
    public ShippingAddress ShippingAddress { get; private set; } = null!;

    private Customer() { } // EF Core

    public static Customer Create(
        string firstName, string lastName, string email,
        string? phoneNumber, ShippingAddress shippingAddress)
    {
        return new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PhoneNumber = phoneNumber,
            ShippingAddress = shippingAddress
        };
    }
}
