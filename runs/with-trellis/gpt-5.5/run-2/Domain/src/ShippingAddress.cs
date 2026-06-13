namespace OrderManagement.Domain;

/// <summary>Customer shipping address.</summary>
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
    public CountryName Country { get; private set; } = null!;

    private ShippingAddress()
    {
    }

    /// <summary>Create an address from validated fields.</summary>
    public ShippingAddress(Street street, City city, StateProvince state, PostalCode postalCode, CountryName country)
    {
        Street = street;
        City = city;
        State = state;
        PostalCode = postalCode;
        Country = country;
    }

    /// <inheritdoc />
    protected override IEnumerable<IComparable?> GetEqualityComponents()
    {
        yield return Street.Value;
        yield return City.Value;
        yield return State.Value;
        yield return PostalCode.Value;
        yield return Country.Value;
    }
}
