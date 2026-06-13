namespace OrderManagement.Domain;

using System.Text.Json.Serialization;
using Trellis.Primitives;

/// <summary>
/// A shipping address composed of street, city, state, postal code, and country.
/// All fields are required.
/// </summary>
[JsonConverter(typeof(CompositeValueObjectJsonConverter<ShippingAddress>))]
public sealed class ShippingAddress : ValueObject
{
    /// <summary>Street line.</summary>
    public string Street { get; private set; } = null!;

    /// <summary>City.</summary>
    public string City { get; private set; } = null!;

    /// <summary>State or region.</summary>
    public string State { get; private set; } = null!;

    /// <summary>Postal or ZIP code.</summary>
    public string PostalCode { get; private set; } = null!;

    /// <summary>Country.</summary>
    public string Country { get; private set; } = null!;

    // EF Core / converter materialization constructor.
    private ShippingAddress()
    {
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
    /// Creates a validated shipping address. Every field must be non-blank.
    /// </summary>
    public static Result<ShippingAddress> TryCreate(
        string? street, string? city, string? state, string? postalCode, string? country) =>
        street.EnsureNotNullOrWhiteSpace(Error.InvalidInput.ForField("street", "required", "Street is required."))
            .Combine(city.EnsureNotNullOrWhiteSpace(Error.InvalidInput.ForField("city", "required", "City is required.")))
            .Combine(state.EnsureNotNullOrWhiteSpace(Error.InvalidInput.ForField("state", "required", "State is required.")))
            .Combine(postalCode.EnsureNotNullOrWhiteSpace(Error.InvalidInput.ForField("postalCode", "required", "Postal code is required.")))
            .Combine(country.EnsureNotNullOrWhiteSpace(Error.InvalidInput.ForField("country", "required", "Country is required.")))
            .Map((s, c, st, p, co) => new ShippingAddress(s.Trim(), c.Trim(), st.Trim(), p.Trim(), co.Trim()));

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
