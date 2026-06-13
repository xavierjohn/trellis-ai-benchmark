namespace OrderManagement.Api.v2026_11_12.Models;

using OrderManagement.Domain;

/// <summary>A shipping address on the wire.</summary>
/// <param name="Street">Street.</param>
/// <param name="City">City.</param>
/// <param name="State">State.</param>
/// <param name="PostalCode">Postal code.</param>
/// <param name="Country">Country.</param>
public sealed record ShippingAddressDto(string Street, string City, string State, string PostalCode, string Country)
{
    /// <summary>Projects a domain shipping address to its wire representation.</summary>
    public static ShippingAddressDto From(ShippingAddress address) =>
        new(address.Street, address.City, address.State, address.PostalCode, address.Country);
}

/// <summary>Response body for a customer.</summary>
/// <param name="Id">Customer id.</param>
/// <param name="FirstName">First name.</param>
/// <param name="LastName">Last name.</param>
/// <param name="Email">Email address.</param>
/// <param name="PhoneNumber">Optional phone number.</param>
/// <param name="ShippingAddress">Shipping address.</param>
public sealed record CustomerResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    ShippingAddressDto ShippingAddress)
{
    /// <summary>Projects a domain customer to its response representation.</summary>
    public static CustomerResponse From(Customer customer) =>
        new(
            customer.Id.Value,
            customer.FirstName.Value,
            customer.LastName.Value,
            customer.Email.Value,
            customer.PhoneNumber.HasValue ? customer.PhoneNumber.Value.Value : null,
            ShippingAddressDto.From(customer.ShippingAddress));
}
