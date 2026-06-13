namespace OrderManagement.Api.v2026_11_12.Models;

using OrderManagement.Domain;

/// <summary>Shipping address response model.</summary>
public sealed record ShippingAddressResponse
{
    /// <summary>Street line.</summary>
    public string Street { get; init; } = null!;
    /// <summary>City.</summary>
    public string City { get; init; } = null!;
    /// <summary>State or province.</summary>
    public string State { get; init; } = null!;
    /// <summary>Postal code.</summary>
    public string PostalCode { get; init; } = null!;
    /// <summary>Country.</summary>
    public string Country { get; init; } = null!;

    /// <summary>Maps from the domain value object.</summary>
    public static ShippingAddressResponse From(ShippingAddress address) => new()
    {
        Street = address.Street,
        City = address.City,
        State = address.State,
        PostalCode = address.PostalCode,
        Country = address.Country,
    };
}
