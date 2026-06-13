namespace OrderManagement.Domain;

using Trellis.Primitives;

/// <summary>
/// Customer aggregate.
/// </summary>
public partial class Customer : Aggregate<CustomerId>
{
    /// <summary>First name.</summary>
    public FirstName FirstName { get; private set; } = null!;

    /// <summary>Last name.</summary>
    public LastName LastName { get; private set; } = null!;

    /// <summary>Email address.</summary>
    public EmailAddress Email { get; private set; } = null!;

    /// <summary>Optional phone number.</summary>
    public partial Maybe<PhoneNumber> PhoneNumber { get; private set; }

    /// <summary>Shipping address.</summary>
    public ShippingAddress ShippingAddress { get; private set; } = null!;

    private Customer() : base(default!)
    {
    }

    /// <summary>Create a customer from validated value objects.</summary>
    public Customer(
        FirstName firstName,
        LastName lastName,
        EmailAddress email,
        Maybe<PhoneNumber> phoneNumber,
        ShippingAddress shippingAddress,
        TimeProvider timeProvider) : base(CustomerId.NewUniqueV7(timeProvider))
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        PhoneNumber = phoneNumber;
        ShippingAddress = shippingAddress;
    }
}
