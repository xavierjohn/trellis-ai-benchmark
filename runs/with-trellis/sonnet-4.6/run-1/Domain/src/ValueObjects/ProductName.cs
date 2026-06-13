namespace OrderManagement.Domain;

/// <summary>
/// Product display name.
/// </summary>
[StringLength(200, MinimumLength = 1)]
public partial class ProductName : RequiredString<ProductName>
{
}
