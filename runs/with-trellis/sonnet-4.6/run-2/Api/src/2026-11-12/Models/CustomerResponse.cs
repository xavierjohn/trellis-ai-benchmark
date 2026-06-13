namespace OrderManagement.Api.v2026_11_12.Models;

using OrderManagement.Domain;

/// <summary>Response model for a customer.</summary>
public record CustomerResponse
{
    /// <summary>Customer's unique identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Customer's first name.</summary>
    public string FirstName { get; init; } = null!;

    /// <summary>Customer's last name.</summary>
    public string LastName { get; init; } = null!;

    /// <summary>Customer's email address.</summary>
    public string Email { get; init; } = null!;

    /// <summary>Customer's optional phone number.</summary>
    public string? PhoneNumber { get; init; }

    /// <summary>Customer's shipping address.</summary>
    public ShippingAddressResponse ShippingAddress { get; init; } = null!;

    /// <summary>When the customer was created.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>Maps from domain aggregate to API response.</summary>
    public static CustomerResponse From(Customer customer) => new()
    {
        Id = customer.Id.Value,
        FirstName = customer.FirstName.Value,
        LastName = customer.LastName.Value,
        Email = customer.Email.Value,
        PhoneNumber = customer.Phone.Match<string?>(p => p.Value, () => null),
        ShippingAddress = new ShippingAddressResponse
        {
            Street = customer.ShippingAddress.Street,
            City = customer.ShippingAddress.City,
            State = customer.ShippingAddress.State,
            PostalCode = customer.ShippingAddress.PostalCode,
            Country = customer.ShippingAddress.Country
        },
        CreatedAt = customer.CreatedAt
    };
}

/// <summary>Shipping address in a response.</summary>
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
}
