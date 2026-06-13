using OrderManagement.Domain.Common;

namespace OrderManagement.Domain.Customers;

/// <summary>Shipping address value object. All fields required.</summary>
public sealed record ShippingAddress
{
    public string Street { get; }
    public string City { get; }
    public string State { get; }
    public string PostalCode { get; }
    public string Country { get; }

    private ShippingAddress(string street, string city, string state, string postalCode, string country)
    {
        Street = street;
        City = city;
        State = state;
        PostalCode = postalCode;
        Country = country;
    }

    public static Result<ShippingAddress> Create(
        string? street, string? city, string? state, string? postalCode, string? country)
    {
        var fields = new Dictionary<string, string[]>();

        void Require(string name, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                fields[name] = new[] { $"{name} is required." };
        }

        Require(nameof(Street), street);
        Require(nameof(City), city);
        Require(nameof(State), state);
        Require(nameof(PostalCode), postalCode);
        Require(nameof(Country), country);

        if (fields.Count > 0)
            return Error.Validation("Shipping address is invalid.", fields);

        return new ShippingAddress(
            street!.Trim(), city!.Trim(), state!.Trim(), postalCode!.Trim(), country!.Trim());
    }
}
