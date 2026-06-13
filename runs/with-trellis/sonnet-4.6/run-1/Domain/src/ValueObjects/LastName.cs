namespace OrderManagement.Domain;

/// <summary>
/// Customer last name.
/// </summary>
[StringLength(100)]
public partial class LastName : RequiredString<LastName>
{
}
