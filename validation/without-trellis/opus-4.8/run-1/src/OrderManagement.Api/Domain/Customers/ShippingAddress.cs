namespace OrderManagement.Api.Domain.Customers;

/// <summary>Shipping address value object. All fields required.</summary>
public sealed class ShippingAddress
{
    private ShippingAddress() { } // EF

    public ShippingAddress(string street, string city, string state, string postalCode, string country)
    {
        Street = street;
        City = city;
        State = state;
        PostalCode = postalCode;
        Country = country;
    }

    public string Street { get; private set; } = default!;
    public string City { get; private set; } = default!;
    public string State { get; private set; } = default!;
    public string PostalCode { get; private set; } = default!;
    public string Country { get; private set; } = default!;
}
