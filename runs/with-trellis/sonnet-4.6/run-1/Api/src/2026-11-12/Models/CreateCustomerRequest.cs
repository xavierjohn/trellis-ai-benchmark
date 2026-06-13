namespace OrderManagement.Api.v2026_11_12.Models;

using OrderManagement.Domain;
using Trellis.Primitives;

/// <summary>Create customer request body.</summary>
public sealed record CreateCustomerRequest
{
    /// <summary>First name.</summary>
    public FirstName FirstName { get; init; } = null!;
    /// <summary>Last name.</summary>
    public LastName LastName { get; init; } = null!;
    /// <summary>Email address.</summary>
    public EmailAddress Email { get; init; } = null!;
    /// <summary>Optional phone number.</summary>
    public string? PhoneNumber { get; init; }
    /// <summary>Shipping address.</summary>
    public CreateShippingAddressRequest ShippingAddress { get; init; } = new();
}
