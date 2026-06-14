namespace OrderManagement.Domain;

using Trellis.Primitives;

/// <summary>Customer aggregate.</summary>
public partial class Customer : Aggregate<CustomerId>
{
    private Customer() : base(default!)
    {
        FirstName = null!;
        LastName = null!;
        Email = null!;
        ShippingAddress = null!;
    }

    private Customer(FirstName firstName, LastName lastName, EmailAddress email, Maybe<PhoneNumber> phoneNumber, ShippingAddress shippingAddress)
        : base(CustomerId.NewUniqueV7())
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        PhoneNumber = phoneNumber;
        ShippingAddress = shippingAddress;
    }

    /// <summary>First name.</summary>
    public FirstName FirstName { get; private set; }

    /// <summary>Last name.</summary>
    public LastName LastName { get; private set; }

    /// <summary>Unique email address.</summary>
    public EmailAddress Email { get; private set; }

    /// <summary>Optional phone number.</summary>
    public partial Maybe<PhoneNumber> PhoneNumber { get; private set; }

    /// <summary>Shipping address.</summary>
    public ShippingAddress ShippingAddress { get; private set; }

    /// <summary>Creates a customer.</summary>
    public static Result<Customer> TryCreate(
        FirstName firstName,
        LastName lastName,
        EmailAddress email,
        Maybe<PhoneNumber> phoneNumber,
        ShippingAddress shippingAddress) =>
        Result.Ok(new Customer(firstName, lastName, email, phoneNumber, shippingAddress));
}
