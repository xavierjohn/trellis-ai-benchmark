namespace OrderManagement.Domain;

using System.Text.Json.Serialization;
using Trellis.Primitives;

/// <summary>
/// A shipping address. Street, city, state, postal code, and country are all required.
/// </summary>
/// <remarks>
/// EF-free composite value object: declares a private parameterless constructor so the
/// Trellis <c>CompositeValueObjectConvention</c> can materialize it without referencing
/// <c>Trellis.EntityFrameworkCore</c> from the Domain layer.
/// </remarks>
[JsonConverter(typeof(CompositeValueObjectJsonConverter<ShippingAddress>))]
public partial class ShippingAddress : ValueObject
{
    /// <summary>Street line.</summary>
    public string Street { get; private set; } = null!;

    /// <summary>City.</summary>
    public string City { get; private set; } = null!;

    /// <summary>State or region.</summary>
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

    /// <summary>Creates a validated shipping address. Every field is required.</summary>
    public static Result<ShippingAddress> TryCreate(
        string? street, string? city, string? state, string? postalCode, string? country, string? fieldName = null)
    {
        var violations = new List<FieldViolation>(5);
        AddIfBlank(violations, street, fieldName, nameof(Street));
        AddIfBlank(violations, city, fieldName, nameof(City));
        AddIfBlank(violations, state, fieldName, nameof(State));
        AddIfBlank(violations, postalCode, fieldName, nameof(PostalCode));
        AddIfBlank(violations, country, fieldName, nameof(Country));

        return violations.Count > 0
            ? Result.Fail<ShippingAddress>(new Error.InvalidInput(EquatableArray.Create(violations.ToArray())))
            : Result.Ok(new ShippingAddress(street!.Trim(), city!.Trim(), state!.Trim(), postalCode!.Trim(), country!.Trim()));
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

    private static void AddIfBlank(List<FieldViolation> violations, string? value, string? owner, string part)
    {
        if (!string.IsNullOrWhiteSpace(value))
            return;

        var leaf = char.ToLowerInvariant(part[0]) + part[1..];
        var pointer = string.IsNullOrWhiteSpace(owner)
            ? InputPointer.ForProperty(leaf)
            : new InputPointer($"/{owner}/{leaf}");
        violations.Add(new FieldViolation(pointer, "required") { Detail = $"{part} is required." });
    }
}
