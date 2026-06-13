namespace OrderManagement.Api.v2026_11_12.Models;

/// <summary>Shipping address payload.</summary>
public sealed record CreateShippingAddressRequest
{
    /// <summary>Street line.</summary>
    public string? Street { get; init; }
    /// <summary>City.</summary>
    public string? City { get; init; }
    /// <summary>State or province.</summary>
    public string? State { get; init; }
    /// <summary>Postal code.</summary>
    public string? PostalCode { get; init; }
    /// <summary>Country.</summary>
    public string? Country { get; init; }
}
