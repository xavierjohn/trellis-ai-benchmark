namespace OrderManagement.Api.v2026_11_12.Models;

using OrderManagement.Domain;
using Trellis.Primitives;

/// <summary>Request body for creating a customer.</summary>
/// <param name="FirstName">First name.</param>
/// <param name="LastName">Last name.</param>
/// <param name="Email">Email address (unique across customers).</param>
/// <param name="Phone">Optional phone number.</param>
/// <param name="ShippingAddress">Shipping address.</param>
public sealed record CreateCustomerRequest(
    FirstName FirstName,
    LastName LastName,
    EmailAddress Email,
    Maybe<PhoneNumber> Phone,
    AddressRequest ShippingAddress);

/// <summary>Shipping address request fields.</summary>
/// <param name="Street">Street line.</param>
/// <param name="City">City.</param>
/// <param name="State">State or region.</param>
/// <param name="PostalCode">Postal or ZIP code.</param>
/// <param name="Country">Country.</param>
public sealed record AddressRequest(string? Street, string? City, string? State, string? PostalCode, string? Country)
{
    /// <summary>Validates and converts to a domain <see cref="ShippingAddress"/>.</summary>
    public Result<ShippingAddress> ToShippingAddress() =>
        ShippingAddress.TryCreate(Street, City, State, PostalCode, Country);
}
