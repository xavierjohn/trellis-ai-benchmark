namespace OrderManagement.Domain;

using Trellis.Primitives;

/// <summary>
/// Customer aggregate.
/// </summary>
public partial class Customer : Aggregate<CustomerId>
{
    /// <summary>Customer first name.</summary>
    public FirstName FirstName { get; private set; } = null!;

    /// <summary>Customer last name.</summary>
    public LastName LastName { get; private set; } = null!;

    /// <summary>Customer email address.</summary>
    public EmailAddress Email { get; private set; } = null!;

    /// <summary>Optional customer phone number.</summary>
    public partial Maybe<PhoneNumber> PhoneNumber { get; private set; }

    /// <summary>Shipping address.</summary>
    public ShippingAddress ShippingAddress { get; private set; } = null!;

    private Customer() : base(default!)
    {
    }

    /// <summary>
    /// Creates a new customer.
    /// </summary>
    public Customer(FirstName firstName, LastName lastName, EmailAddress email, Maybe<PhoneNumber> phoneNumber, ShippingAddress shippingAddress)
        : base(CustomerId.NewUniqueV7())
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        PhoneNumber = phoneNumber;
        ShippingAddress = shippingAddress;
    }
}
