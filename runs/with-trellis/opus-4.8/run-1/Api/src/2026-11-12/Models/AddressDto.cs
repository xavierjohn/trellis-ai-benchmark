namespace OrderManagement.Api.v2026_11_12.Models;

using OrderManagement.Domain;

/// <summary>Shipping address payload.</summary>
/// <param name="Street">Street line.</param>
/// <param name="City">City.</param>
/// <param name="State">State or region.</param>
/// <param name="PostalCode">Postal or ZIP code.</param>
/// <param name="Country">Country.</param>
public sealed record AddressDto(string Street, string City, string State, string PostalCode, string Country)
{
    /// <summary>Projects a domain <see cref="ShippingAddress"/> to a DTO.</summary>
    public static AddressDto From(ShippingAddress address) =>
        new(address.Street, address.City, address.State, address.PostalCode, address.Country);
}
