namespace OrderManagement.Api.v2026_11_12.Models;

using OrderManagement.Domain;

/// <summary>Customer response model.</summary>
public sealed record CustomerResponse
{
    /// <summary>Customer identifier.</summary>
    public Guid Id { get; init; }
    /// <summary>First name.</summary>
    public string FirstName { get; init; } = null!;
    /// <summary>Last name.</summary>
    public string LastName { get; init; } = null!;
    /// <summary>Email address.</summary>
    public string Email { get; init; } = null!;
    /// <summary>Phone number when present.</summary>
    public string? PhoneNumber { get; init; }
    /// <summary>Shipping address.</summary>
    public ShippingAddressResponse ShippingAddress { get; init; } = null!;
    /// <summary>Created timestamp.</summary>
    public DateTimeOffset CreatedAt { get; init; }
    /// <summary>Last modified timestamp.</summary>
    public DateTimeOffset LastModified { get; init; }

    /// <summary>Maps from the domain aggregate.</summary>
    public static CustomerResponse From(Customer customer) => new()
    {
        Id = customer.Id.Value,
        FirstName = customer.FirstName.Value,
        LastName = customer.LastName.Value,
        Email = customer.Email.Value,
        PhoneNumber = customer.PhoneNumber.TryGetValue(out var phoneNumber) ? phoneNumber.Value : null,
        ShippingAddress = ShippingAddressResponse.From(customer.ShippingAddress),
        CreatedAt = customer.CreatedAt,
        LastModified = customer.LastModified,
    };
}
