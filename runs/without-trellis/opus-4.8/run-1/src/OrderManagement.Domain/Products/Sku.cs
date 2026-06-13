using System.Text.RegularExpressions;
using OrderManagement.Domain.Common;

namespace OrderManagement.Domain.Products;

/// <summary>Stock Keeping Unit value object: 3–20 chars, uppercase letters and digits only.</summary>
public sealed partial class Sku
{
    public string Value { get; }

    private Sku(string value) => Value = value;

    public static Result<Sku> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Error.Validation(nameof(Sku), "SKU is required.");

        var trimmed = value.Trim();
        if (!SkuRegex().IsMatch(trimmed))
            return Error.Validation(nameof(Sku),
                "SKU must be 3–20 characters, uppercase letters and digits only.");

        return new Sku(trimmed);
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[A-Z0-9]{3,20}$", RegexOptions.CultureInvariant)]
    private static partial Regex SkuRegex();
}
