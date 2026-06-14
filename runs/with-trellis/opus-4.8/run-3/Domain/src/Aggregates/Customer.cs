namespace OrderManagement.Domain;

using Trellis.Primitives;

/// <summary>A customer who places orders.</summary>
public sealed partial class Customer : Aggregate<CustomerId>
{
    /// <summary>The customer's first name.</summary>
    public FirstName FirstName { get; private set; } = null!;

    /// <summary>The customer's last name.</summary>
    public LastName LastName { get; private set; } = null!;

    /// <summary>The customer's unique email address.</summary>
    public EmailAddress Email { get; private set; } = null!;

    /// <summary>The customer's optional phone number.</summary>
    public partial Maybe<PhoneNumber> Phone { get; private set; }

    /// <summary>The customer's shipping address.</summary>
    public ShippingAddress ShippingAddress { get; private set; } = null!;

    private Customer() : base(default!)
    {
    }

    private Customer(
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

    /// <summary>Creates a new customer.</summary>
    public static Customer Create(
        FirstName firstName,
        LastName lastName,
        EmailAddress email,
        Maybe<PhoneNumber> phone,
        ShippingAddress shippingAddress) =>
        new(firstName, lastName, email, phone, shippingAddress);
}
