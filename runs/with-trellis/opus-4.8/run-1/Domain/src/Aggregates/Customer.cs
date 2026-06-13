namespace OrderManagement.Domain;

using Trellis.Primitives;

/// <summary>
/// A customer that places orders. Identified by <see cref="CustomerId"/>.
/// </summary>
public partial class Customer : Aggregate<CustomerId>
{
    /// <summary>Customer first name.</summary>
    public FirstName FirstName { get; private set; } = null!;

    /// <summary>Customer last name.</summary>
    public LastName LastName { get; private set; } = null!;

    /// <summary>Email address. Unique across all customers.</summary>
    public EmailAddress Email { get; private set; } = null!;

    /// <summary>Optional phone number.</summary>
    public partial Maybe<PhoneNumber> Phone { get; private set; }

    /// <summary>Shipping address.</summary>
    public ShippingAddress ShippingAddress { get; private set; } = null!;

    /// <summary>EF Core constructor.</summary>
    private Customer() : base(default!)
    {
    }

    /// <summary>
    /// Creates a new customer.
    /// </summary>
    public Customer(
        FirstName firstName,
        LastName lastName,
        EmailAddress email,
        Maybe<PhoneNumber> phone,
        ShippingAddress shippingAddress)
        : base(CustomerId.NewUniqueV7())
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Phone = phone;
        ShippingAddress = shippingAddress;
    }
}
