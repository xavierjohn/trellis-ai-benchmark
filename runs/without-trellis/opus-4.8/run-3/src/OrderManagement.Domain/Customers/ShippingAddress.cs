using System.Text.RegularExpressions;
using OrderManagement.Domain.Common;

namespace OrderManagement.Domain.Customers;

/// <summary>A postal shipping address. All fields are required.</summary>
public sealed partial record ShippingAddress
{
    private ShippingAddress(string street, string city, string state, string postalCode, string country)
    {
        Street = street;
        City = city;
        State = state;
        PostalCode = postalCode;
        Country = country;
    }

    public string Street { get; }
    public string City { get; }
    public string State { get; }
    public string PostalCode { get; }
    public string Country { get; }

    public static Result<ShippingAddress> Create(string? street, string? city, string? state, string? postalCode, string? country)
    {
        var fieldErrors = new Dictionary<string, string[]>();

        void Require(string name, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                fieldErrors[name] = [$"{name} is required."];
        }

        Require("street", street);
        Require("city", city);
        Require("state", state);
        Require("postalCode", postalCode);
        Require("country", country);

        if (fieldErrors.Count > 0)
            return Error.Validation(fieldErrors, "Shipping address is invalid.");

        return new ShippingAddress(street!.Trim(), city!.Trim(), state!.Trim(), postalCode!.Trim(), country!.Trim());
    }
}
