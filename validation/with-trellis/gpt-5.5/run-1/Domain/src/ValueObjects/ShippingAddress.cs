namespace OrderManagement.Domain;

/// <summary>Customer shipping address.</summary>
public sealed class ShippingAddress : ValueObject
{
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

    /// <summary>Street address.</summary>
    public string Street { get; private set; }

    /// <summary>City.</summary>
    public string City { get; private set; }

    /// <summary>State or region.</summary>
    public string State { get; private set; }

    /// <summary>Postal code.</summary>
    public string PostalCode { get; private set; }

    /// <summary>Country.</summary>
    public string Country { get; private set; }

    /// <summary>Creates a validated shipping address.</summary>
    public static Result<ShippingAddress> TryCreate(
        string? street,
        string? city,
        string? state,
        string? postalCode,
        string? country)
    {
        static Result<string> Required(string? value, string field) =>
            string.IsNullOrWhiteSpace(value)
                ? Result.Fail<string>(Error.InvalidInput.ForField(field, "required", $"{field} is required."))
                : Result.Ok(value.Trim());

        return Required(street, "shippingAddress.street")
            .Combine(Required(city, "shippingAddress.city"))
            .Combine(Required(state, "shippingAddress.state"))
            .Combine(Required(postalCode, "shippingAddress.postalCode"))
            .Combine(Required(country, "shippingAddress.country"))
            .Map((s, c, st, p, co) => new ShippingAddress(s, c, st, p, co));
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
