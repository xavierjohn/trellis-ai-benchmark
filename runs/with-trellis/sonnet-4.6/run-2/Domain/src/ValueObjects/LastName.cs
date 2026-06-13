namespace OrderManagement.Domain;

/// <summary>A person's last name.</summary>
[StringLength(100)]
public partial class LastName : RequiredString<LastName> { }
