namespace OrderManagement.Api.v2026_11_12.Models;

using OrderManagement.Domain;
using Trellis.Primitives;

/// <summary>
/// Shipping address request.
/// </summary>
public sealed record ShippingAddressRequest
{
    /// <summary>Street address.</summary>
    public Street Street { get; init; } = null!;

    /// <summary>City.</summary>
    public City City { get; init; } = null!;

    /// <summary>State or province.</summary>
    public StateProvince State { get; init; } = null!;

    /// <summary>Postal code.</summary>
    public PostalCode PostalCode { get; init; } = null!;

    /// <summary>Country.</summary>
    public Country Country { get; init; } = null!;

    /// <summary>Converts the request to a domain address.</summary>
    public ShippingAddress ToDomain() => new(Street, City, State, PostalCode, Country);
}

/// <summary>
/// Create customer request.
/// </summary>
public sealed record CreateCustomerRequest
{
    /// <summary>First name.</summary>
    public FirstName FirstName { get; init; } = null!;

    /// <summary>Last name.</summary>
    public LastName LastName { get; init; } = null!;

    /// <summary>Email address.</summary>
    public EmailAddress Email { get; init; } = null!;

    /// <summary>Optional phone number.</summary>
    public Maybe<PhoneNumber> PhoneNumber { get; init; }

    /// <summary>Shipping address.</summary>
    public ShippingAddressRequest ShippingAddress { get; init; } = null!;
}

/// <summary>
/// Shipping address response.
/// </summary>
public sealed record ShippingAddressResponse
{
    /// <summary>Street address.</summary>
    public string Street { get; init; } = null!;

    /// <summary>City.</summary>
    public string City { get; init; } = null!;

    /// <summary>State or province.</summary>
    public string State { get; init; } = null!;

    /// <summary>Postal code.</summary>
    public string PostalCode { get; init; } = null!;

    /// <summary>Country.</summary>
    public string Country { get; init; } = null!;

    /// <summary>Maps a domain address.</summary>
    public static ShippingAddressResponse From(ShippingAddress address) => new()
    {
        Street = address.Street.Value,
        City = address.City.Value,
        State = address.State.Value,
        PostalCode = address.PostalCode.Value,
        Country = address.Country.Value,
    };
}

/// <summary>
/// Customer response.
/// </summary>
public sealed record CustomerResponse
{
    /// <summary>Customer ID.</summary>
    public Guid Id { get; init; }

    /// <summary>First name.</summary>
    public string FirstName { get; init; } = null!;

    /// <summary>Last name.</summary>
    public string LastName { get; init; } = null!;

    /// <summary>Email address.</summary>
    public string Email { get; init; } = null!;

    /// <summary>Optional phone number.</summary>
    public string? PhoneNumber { get; init; }

    /// <summary>Shipping address.</summary>
    public ShippingAddressResponse ShippingAddress { get; init; } = null!;

    /// <summary>Maps a domain customer.</summary>
    public static CustomerResponse From(Customer customer) => new()
    {
        Id = customer.Id.Value,
        FirstName = customer.FirstName.Value,
        LastName = customer.LastName.Value,
        Email = customer.Email.Value,
        PhoneNumber = customer.PhoneNumber.Match<string?>(phone => phone.Value, () => null),
        ShippingAddress = ShippingAddressResponse.From(customer.ShippingAddress),
    };
}
