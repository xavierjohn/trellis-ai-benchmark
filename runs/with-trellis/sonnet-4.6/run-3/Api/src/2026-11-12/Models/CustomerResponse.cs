namespace OrderManagement.Api.v2026_11_12.Models;

using OrderManagement.Domain;

/// <summary>Response model for a customer.</summary>
public record CustomerResponse
{
    /// <summary>Unique identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>First name.</summary>
    public string FirstName { get; init; } = null!;

    /// <summary>Last name.</summary>
    public string LastName { get; init; } = null!;

    /// <summary>Email address.</summary>
    public string Email { get; init; } = null!;

    /// <summary>Phone number, if provided.</summary>
    public string? PhoneNumber { get; init; }

    /// <summary>Shipping address.</summary>
    public ShippingAddressResponse ShippingAddress { get; init; } = null!;

    /// <summary>When created.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>When last modified.</summary>
    public DateTimeOffset LastModified { get; init; }

    /// <summary>Maps from domain aggregate.</summary>
    public static CustomerResponse From(Customer customer) => new()
    {
        Id = customer.Id.Value,
        FirstName = customer.FirstName.Value,
        LastName = customer.LastName.Value,
        Email = customer.Email.Value,
        PhoneNumber = customer.PhoneNumber.Match<string?>(phoneNumber => phoneNumber.Value, () => null),
        ShippingAddress = ShippingAddressResponse.From(customer.ShippingAddress),
        CreatedAt = customer.CreatedAt,
        LastModified = customer.LastModified,
    };
}

/// <summary>Response model for a shipping address.</summary>
public record ShippingAddressResponse
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

    /// <summary>Maps from domain value object.</summary>
    public static ShippingAddressResponse From(ShippingAddress address) => new()
    {
        Street = address.Street,
        City = address.City,
        State = address.State,
        PostalCode = address.PostalCode,
        Country = address.Country,
    };
}
