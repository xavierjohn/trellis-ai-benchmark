namespace OrderManagement.Api.v2026_11_12.Models;

using OrderManagement.Domain;
using Trellis.Primitives;

/// <summary>Response body representing a customer.</summary>
/// <param name="Id">Customer identifier.</param>
/// <param name="FirstName">First name.</param>
/// <param name="LastName">Last name.</param>
/// <param name="Email">Email address.</param>
/// <param name="Phone">Phone number, when present.</param>
/// <param name="ShippingAddress">Shipping address.</param>
public sealed record CustomerResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    AddressDto ShippingAddress)
{
    /// <summary>Projects a domain <see cref="Customer"/> to a response.</summary>
    public static CustomerResponse From(Customer customer) =>
        new(
            customer.Id.Value,
            customer.FirstName.Value,
            customer.LastName.Value,
            customer.Email.Value,
            customer.Phone.TryGetValue(out var phone) ? phone.Value : null,
            AddressDto.From(customer.ShippingAddress));
}
