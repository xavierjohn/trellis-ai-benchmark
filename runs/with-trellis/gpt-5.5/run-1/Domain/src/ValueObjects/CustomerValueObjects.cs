namespace OrderManagement.Domain;

/// <summary>
/// Customer first name.
/// </summary>
[StringLength(100, MinimumLength = 1)]
public partial class FirstName : RequiredString<FirstName>
{
}

/// <summary>
/// Customer last name.
/// </summary>
[StringLength(100, MinimumLength = 1)]
public partial class LastName : RequiredString<LastName>
{
}

/// <summary>
/// Street address line.
/// </summary>
[StringLength(200, MinimumLength = 1)]
public partial class Street : RequiredString<Street>
{
}

/// <summary>
/// City name.
/// </summary>
[StringLength(100, MinimumLength = 1)]
public partial class City : RequiredString<City>
{
}

/// <summary>
/// State or province.
/// </summary>
[StringLength(100, MinimumLength = 1)]
public partial class StateProvince : RequiredString<StateProvince>
{
}

/// <summary>
/// Postal code.
/// </summary>
[StringLength(20, MinimumLength = 1)]
public partial class PostalCode : RequiredString<PostalCode>
{
}

/// <summary>
/// Country name.
/// </summary>
[StringLength(100, MinimumLength = 1)]
public partial class Country : RequiredString<Country>
{
}
