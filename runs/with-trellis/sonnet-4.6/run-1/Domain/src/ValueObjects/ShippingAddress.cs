namespace OrderManagement.Domain;

/// <summary>
/// Shipping address for a customer.
/// </summary>
public class ShippingAddress : ValueObject
{
    /// <summary>Street line.</summary>
    public string Street { get; private set; } = null!;

    /// <summary>City.</summary>
    public string City { get; private set; } = null!;

    /// <summary>State or province.</summary>
    public string State { get; private set; } = null!;

    /// <summary>Postal code.</summary>
    public string PostalCode { get; private set; } = null!;

    /// <summary>Country.</summary>
    public string Country { get; private set; } = null!;

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
    /// Creates a validated shipping address.
    /// </summary>
    public static Result<ShippingAddress> TryCreate(string? street, string? city, string? state, string? postalCode, string? country)
    {
        var streetResult = Result.Ensure(!string.IsNullOrWhiteSpace(street), Error.InvalidInput.ForField("shippingAddress.street", "required", "Street is required."));
        var cityResult = Result.Ensure(!string.IsNullOrWhiteSpace(city), Error.InvalidInput.ForField("shippingAddress.city", "required", "City is required."));
        var stateResult = Result.Ensure(!string.IsNullOrWhiteSpace(state), Error.InvalidInput.ForField("shippingAddress.state", "required", "State is required."));
        var postalResult = Result.Ensure(!string.IsNullOrWhiteSpace(postalCode), Error.InvalidInput.ForField("shippingAddress.postalCode", "required", "Postal code is required."));
        var countryResult = Result.Ensure(!string.IsNullOrWhiteSpace(country), Error.InvalidInput.ForField("shippingAddress.country", "required", "Country is required."));

        return streetResult
            .Combine(cityResult)
            .Combine(stateResult)
            .Combine(postalResult)
            .Combine(countryResult)
            .Map(_ => new ShippingAddress(street!, city!, state!, postalCode!, country!));
    }

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
