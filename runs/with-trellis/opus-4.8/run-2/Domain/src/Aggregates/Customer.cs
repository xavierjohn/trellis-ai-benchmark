namespace OrderManagement.Domain;

using Trellis.Primitives;

/// <summary>
/// A customer who places orders.
/// </summary>
public partial class Customer : Aggregate<CustomerId>
{
    /// <summary>First name.</summary>
    public FirstName FirstName { get; private set; } = null!;

    /// <summary>Last name.</summary>
    public LastName LastName { get; private set; } = null!;

    /// <summary>Email address (unique across all customers).</summary>
    public EmailAddress Email { get; private set; } = null!;

    /// <summary>Optional phone number.</summary>
    public partial Maybe<PhoneNumber> PhoneNumber { get; private set; }

    /// <summary>Shipping address.</summary>
    public ShippingAddress ShippingAddress { get; private set; } = null!;

    /// <summary>EF Core constructor.</summary>
    private Customer() : base(default!) { }

    private Customer(CustomerId id, FirstName firstName, LastName lastName, EmailAddress email, Maybe<PhoneNumber> phoneNumber, ShippingAddress shippingAddress)
        : base(id)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        PhoneNumber = phoneNumber;
        ShippingAddress = shippingAddress;
    }

    /// <summary>
    /// Creates a new customer. All value objects are validated before this call.
    /// </summary>
    public static Customer Create(FirstName firstName, LastName lastName, EmailAddress email, Maybe<PhoneNumber> phoneNumber, ShippingAddress shippingAddress) =>
        new(CustomerId.NewUniqueV7(), firstName, lastName, email, phoneNumber, shippingAddress);
}
