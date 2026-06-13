using System.Text.RegularExpressions;
using OrderManagement.Domain.Common;

namespace OrderManagement.Domain.Customers;

public sealed partial class Customer
{
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"^\+?[0-9\s\-().]{7,20}$")]
    private static partial Regex PhoneRegex();

    private Customer(
        Guid id,
        string firstName,
        string lastName,
        string email,
        string? phoneNumber,
        ShippingAddress shippingAddress)
    {
        Id = id;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        PhoneNumber = phoneNumber;
        ShippingAddress = shippingAddress;
    }

    // Parameterless ctor for EF Core materialization.
    private Customer() { }

    public Guid Id { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }
    public ShippingAddress ShippingAddress { get; private set; } = null!;

    public static Result<Customer> Create(
        string? firstName,
        string? lastName,
        string? email,
        string? phoneNumber,
        ShippingAddress? shippingAddress)
    {
        var fieldErrors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(firstName) || firstName.Trim().Length > 100)
            fieldErrors["firstName"] = ["firstName is required and must be 1-100 characters."];

        if (string.IsNullOrWhiteSpace(lastName) || lastName.Trim().Length > 100)
            fieldErrors["lastName"] = ["lastName is required and must be 1-100 characters."];

        if (string.IsNullOrWhiteSpace(email) || !EmailRegex().IsMatch(email.Trim()))
            fieldErrors["email"] = ["A valid email address is required."];

        var normalizedPhone = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
        if (normalizedPhone is not null && !PhoneRegex().IsMatch(normalizedPhone))
            fieldErrors["phoneNumber"] = ["phoneNumber must be a valid phone number."];

        if (shippingAddress is null)
            fieldErrors["shippingAddress"] = ["shippingAddress is required."];

        if (fieldErrors.Count > 0)
            return Error.Validation(fieldErrors);

        return new Customer(
            Guid.NewGuid(),
            firstName!.Trim(),
            lastName!.Trim(),
            email!.Trim(),
            normalizedPhone,
            shippingAddress!);
    }
}
