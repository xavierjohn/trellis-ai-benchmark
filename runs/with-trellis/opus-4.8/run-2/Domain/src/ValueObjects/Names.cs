namespace OrderManagement.Domain;

/// <summary>Customer first name. 1–100 characters.</summary>
[StringLength(100)]
public sealed partial class FirstName : RequiredString<FirstName>;

/// <summary>Customer last name. 1–100 characters.</summary>
[StringLength(100)]
public sealed partial class LastName : RequiredString<LastName>;

/// <summary>Product name. 1–200 characters.</summary>
[StringLength(200)]
public sealed partial class ProductName : RequiredString<ProductName>;
