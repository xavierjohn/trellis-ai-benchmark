namespace OrderManagement.Domain;

/// <summary>
/// Customer shipping address.
/// </summary>
public sealed class ShippingAddress : ValueObject
{
    /// <summary>Street address.</summary>
    public string Street { get; private set; } = string.Empty;

    /// <summary>City.</summary>
    public string City { get; private set; } = string.Empty;

    /// <summary>State or province.</summary>
    public string State { get; private set; } = string.Empty;

    /// <summary>Postal code.</summary>
    public string PostalCode { get; private set; } = string.Empty;

    /// <summary>Country.</summary>
    public string Country { get; private set; } = string.Empty;

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

    /// <summary>Create a validated shipping address.</summary>
    public static Result<ShippingAddress> TryCreate(
        string? street,
        string? city,
        string? state,
        string? postalCode,
        string? country) =>
        Result.Combine(
                Required(street, "shippingAddress.street"),
                Required(city, "shippingAddress.city"),
                Required(state, "shippingAddress.state"),
                Required(postalCode, "shippingAddress.postalCode"),
                Required(country, "shippingAddress.country"))
            .Map(parts => new ShippingAddress(parts.Item1, parts.Item2, parts.Item3, parts.Item4, parts.Item5));

    /// <inheritdoc />
    protected override IEnumerable<IComparable?> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return State;
        yield return PostalCode;
        yield return Country;
    }

    private static Result<string> Required(string? value, string fieldName)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed)
            ? Result.Fail<string>(Error.InvalidInput.ForField(fieldName, "required", $"{fieldName} is required."))
            : Result.Ok(trimmed);
    }
}
