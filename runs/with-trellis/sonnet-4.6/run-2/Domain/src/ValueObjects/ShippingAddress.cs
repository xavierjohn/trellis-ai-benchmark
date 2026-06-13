namespace OrderManagement.Domain;

/// <summary>
/// A shipping address composite value object.
/// </summary>
public class ShippingAddress : ValueObject
{
    /// <summary>Street address line.</summary>
    public string Street { get; }

    /// <summary>City.</summary>
    public string City { get; }

    /// <summary>State or province.</summary>
    public string State { get; }

    /// <summary>Postal code.</summary>
    public string PostalCode { get; }

    /// <summary>Country.</summary>
    public string Country { get; }

    /// <summary>EF Core constructor.</summary>
    private ShippingAddress()
    {
        Street = null!;
        City = null!;
        State = null!;
        PostalCode = null!;
        Country = null!;
    }

    private ShippingAddress(string street, string city, string state, string postalCode, string country)
    {
        Street = street;
        City = city;
        State = state;
        PostalCode = postalCode;
        Country = country;
    }

    /// <summary>
    /// Creates a ShippingAddress, returning a failure result if any field is blank.
    /// </summary>
    public static Result<ShippingAddress> TryCreate(
        string? street,
        string? city,
        string? state,
        string? postalCode,
        string? country) =>
        Result.Combine(
            Result.Ensure(!string.IsNullOrWhiteSpace(street), Error.InvalidInput.ForField("street", "required", "Street is required.")),
            Result.Ensure(!string.IsNullOrWhiteSpace(city), Error.InvalidInput.ForField("city", "required", "City is required.")),
            Result.Ensure(!string.IsNullOrWhiteSpace(state), Error.InvalidInput.ForField("state", "required", "State is required.")),
            Result.Ensure(!string.IsNullOrWhiteSpace(postalCode), Error.InvalidInput.ForField("postalCode", "required", "Postal code is required.")),
            Result.Ensure(!string.IsNullOrWhiteSpace(country), Error.InvalidInput.ForField("country", "required", "Country is required.")))
        .Map(_ => new ShippingAddress(street!, city!, state!, postalCode!, country!));

    /// <inheritdoc />
    protected override IEnumerable<IComparable?> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return State;
        yield return PostalCode;
        yield return Country;
    }
}
