namespace OrderManagement.Domain;

/// <summary>
/// Customer first name.
/// </summary>
[StringLength(100)]
public partial class FirstName : RequiredString<FirstName>
{
}
