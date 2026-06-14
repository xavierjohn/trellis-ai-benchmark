namespace OrderManagement.Domain;

/// <summary>
/// Customer shipping address.
/// </summary>
public sealed class ShippingAddress : ValueObject
{
    /// <summary>Street address.</summary>
    public Street Street { get; private set; } = null!;

    /// <summary>City.</summary>
    public City City { get; private set; } = null!;

    /// <summary>State or province.</summary>
    public StateProvince State { get; private set; } = null!;

    /// <summary>Postal code.</summary>
    public PostalCode PostalCode { get; private set; } = null!;

    /// <summary>Country.</summary>
    public Country Country { get; private set; } = null!;

    private ShippingAddress()
    {
    }

    /// <summary>
    /// Creates an address from already-validated fields.
    /// </summary>
    public ShippingAddress(Street street, City city, StateProvince state, PostalCode postalCode, Country country)
    {
        Street = street;
        City = city;
        State = state;
        PostalCode = postalCode;
        Country = country;
    }

    protected override IEnumerable<IComparable?> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return State;
        yield return PostalCode;
        yield return Country;
    }
}
