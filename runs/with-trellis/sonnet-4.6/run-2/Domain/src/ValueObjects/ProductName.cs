namespace OrderManagement.Domain;

/// <summary>Name of a product.</summary>
[StringLength(200)]
public partial class ProductName : RequiredString<ProductName> { }
