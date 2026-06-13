namespace OrderManagement.Domain;

/// <summary>
/// Order line quantity.
/// </summary>
[Range(1, 999)]
public partial class Quantity : RequiredInt<Quantity>
{
}
