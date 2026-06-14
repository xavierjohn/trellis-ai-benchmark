namespace OrderManagement.Domain;

/// <summary>Customer first name.</summary>
[StringLength(100)]
public sealed partial class FirstName : RequiredString<FirstName>;

/// <summary>Customer last name.</summary>
[StringLength(100)]
public sealed partial class LastName : RequiredString<LastName>;

/// <summary>Product name.</summary>
[StringLength(200)]
public sealed partial class ProductName : RequiredString<ProductName>;
