namespace OrderManagement.Domain;

using Trellis.Primitives;

/// <summary>
/// A customer aggregate.
/// </summary>
public partial class Customer : Aggregate<CustomerId>
{
    /// <summary>Customer's first name.</summary>
    public FirstName FirstName { get; private set; } = null!;

    /// <summary>Customer's last name.</summary>
    public LastName LastName { get; private set; } = null!;

    /// <summary>Customer's email address.</summary>
    public EmailAddress Email { get; private set; } = null!;

    /// <summary>Customer's optional phone number.</summary>
    public partial Maybe<PhoneNumber> Phone { get; private set; }

    /// <summary>Customer's shipping address.</summary>
    public ShippingAddress ShippingAddress { get; private set; } = null!;

    /// <summary>EF Core constructor.</summary>
    private Customer() : base(default!) { }

    /// <summary>Creates a new customer.</summary>
    public Customer(
        FirstName firstName,
        LastName lastName,
        EmailAddress email,
        Maybe<PhoneNumber> phone,
        ShippingAddress shippingAddress) : base(CustomerId.NewUniqueV7())
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Phone = phone;
        ShippingAddress = shippingAddress;
    }
}
