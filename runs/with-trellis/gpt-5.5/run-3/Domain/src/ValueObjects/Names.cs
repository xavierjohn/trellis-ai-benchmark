namespace OrderManagement.Domain;

/// <summary>Customer first name.</summary>
[StringLength(100)]
public partial class FirstName : RequiredString<FirstName>;

/// <summary>Customer last name.</summary>
[StringLength(100)]
public partial class LastName : RequiredString<LastName>;

/// <summary>Product display name.</summary>
[StringLength(200)]
public partial class ProductName : RequiredString<ProductName>;
