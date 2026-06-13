using System.Text.RegularExpressions;
using OrderManagement.Domain.Common;

namespace OrderManagement.Domain.Customers;

/// <summary>
/// Shipping address value object. All fields are required.
/// </summary>
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

        if (string.IsNullOrWhiteSpace(street))
            fields["shippingAddress.street"] = ["Street is required."];
        if (string.IsNullOrWhiteSpace(city))
            fields["shippingAddress.city"] = ["City is required."];
        if (string.IsNullOrWhiteSpace(state))
            fields["shippingAddress.state"] = ["State is required."];
        if (string.IsNullOrWhiteSpace(postalCode))
            fields["shippingAddress.postalCode"] = ["Postal code is required."];
        if (string.IsNullOrWhiteSpace(country))
            fields["shippingAddress.country"] = ["Country is required."];

        if (fields.Count > 0)
            return Error.Validation(fields);

        return new ShippingAddress(
            street!.Trim(), city!.Trim(), state!.Trim(), postalCode!.Trim(), country!.Trim());
    }
}
