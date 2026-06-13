namespace OrderManagement.Domain;

/// <summary>A person's first name.</summary>
[StringLength(100)]
public partial class FirstName : RequiredString<FirstName> { }
