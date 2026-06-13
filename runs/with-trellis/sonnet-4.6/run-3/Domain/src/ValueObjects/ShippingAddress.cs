namespace OrderManagement.Domain;

/// <summary>Shipping address value object.</summary>
public sealed class ShippingAddress : ValueObject
{
    /// <summary>Street address.</summary>
    public string Street { get; private set; } = null!;

    /// <summary>City.</summary>
    public string City { get; private set; } = null!;

    /// <summary>State.</summary>
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

    public static Result<ShippingAddress> TryCreate(string? street, string? city, string? state, string? postalCode, string? country)
    {
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(street))
            errors.Add(Error.InvalidInput.ForField("shippingAddress.street", "required", "Street is required."));

        if (string.IsNullOrWhiteSpace(city))
            errors.Add(Error.InvalidInput.ForField("shippingAddress.city", "required", "City is required."));

        if (string.IsNullOrWhiteSpace(state))
            errors.Add(Error.InvalidInput.ForField("shippingAddress.state", "required", "State is required."));

        if (string.IsNullOrWhiteSpace(postalCode))
            errors.Add(Error.InvalidInput.ForField("shippingAddress.postalCode", "required", "Postal code is required."));

        if (string.IsNullOrWhiteSpace(country))
            errors.Add(Error.InvalidInput.ForField("shippingAddress.country", "required", "Country is required."));

        if (errors.Count > 0)
        {
            var combined = errors.Skip(1).Aggregate(errors[0], (current, next) => current.Combine(next));
            return Result.Fail<ShippingAddress>(combined);
        }

        return Result.Ok(new ShippingAddress(street!, city!, state!, postalCode!, country!));
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
