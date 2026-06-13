namespace OrderManagement.Domain;

/// <summary>Customer aggregate.</summary>
public partial class Customer : Aggregate<CustomerId>
{
    public FirstName FirstName { get; private set; } = null!;
    public LastName LastName { get; private set; } = null!;
    public Email Email { get; private set; } = null!;
    public partial Maybe<PhoneNumber> PhoneNumber { get; private set; }
    public ShippingAddress ShippingAddress { get; private set; } = null!;

    /// <summary>EF Core constructor.</summary>
    private Customer()
        : base(default!)
    {
    }

    /// <summary>Creates a new customer.</summary>
    public Customer(FirstName firstName, LastName lastName, Email email, Maybe<PhoneNumber> phoneNumber, ShippingAddress shippingAddress)
        : base(CustomerId.NewUniqueV7())
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        PhoneNumber = phoneNumber;
        ShippingAddress = shippingAddress;
    }

    /// <summary>Full name.</summary>
    public string FullName => $"{FirstName.Value} {LastName.Value}";
}
