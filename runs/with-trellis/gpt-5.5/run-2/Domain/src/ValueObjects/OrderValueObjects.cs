namespace OrderManagement.Domain;

using System.Text.RegularExpressions;

/// <summary>Customer first name.</summary>
[StringLength(100)]
public partial class FirstName : RequiredString<FirstName>;

/// <summary>Customer last name.</summary>
[StringLength(100)]
public partial class LastName : RequiredString<LastName>;

/// <summary>Product display name.</summary>
[StringLength(200)]
public partial class ProductName : RequiredString<ProductName>;

/// <summary>Stock keeping unit.</summary>
[StringLength(20, MinimumLength = 3)]
public partial class Sku : RequiredString<Sku>
{
    private static readonly Regex Pattern = new("^[A-Z0-9]{3,20}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    static partial void ValidateAdditional(string value, string fieldName, ref string? errorMessage)
    {
        if (!Pattern.IsMatch(value))
            errorMessage = "SKU must be 3-20 uppercase letters and digits.";
    }
}

/// <summary>Product unit price in USD.</summary>
[Range(0.01, 1_000_000)]
public partial class UnitPrice : RequiredDecimal<UnitPrice>;

/// <summary>Available stock quantity.</summary>
[AllowZero]
[Range(0, 1_000_000)]
public partial class StockQuantity : RequiredInt<StockQuantity>;

/// <summary>Order line quantity.</summary>
[Range(1, 999)]
public partial class OrderQuantity : RequiredInt<OrderQuantity>;

/// <summary>Required street value.</summary>
[StringLength(200)]
public partial class Street : RequiredString<Street>;

/// <summary>Required city value.</summary>
[StringLength(100)]
public partial class City : RequiredString<City>;

/// <summary>Required state value.</summary>
[StringLength(100)]
public partial class StateProvince : RequiredString<StateProvince>;

/// <summary>Required postal code value.</summary>
[StringLength(20)]
public partial class PostalCode : RequiredString<PostalCode>;

/// <summary>Required country value.</summary>
[StringLength(100)]
public partial class CountryName : RequiredString<CountryName>;
