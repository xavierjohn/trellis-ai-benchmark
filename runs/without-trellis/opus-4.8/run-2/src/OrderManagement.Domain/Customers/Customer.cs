using System.Text.RegularExpressions;
using OrderManagement.Domain.Common;

namespace OrderManagement.Domain.Customers;

public sealed class Customer
{
    // Simple, pragmatic email/phone validation. Not exhaustive RFC compliance.
    private static readonly Regex EmailRegex =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    private static readonly Regex PhoneRegex =
        new(@"^\+?[0-9\s\-\(\)]{7,20}$", RegexOptions.Compiled);

    public Guid Id { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }
    public ShippingAddress ShippingAddress { get; private set; } = null!;

    private Customer() { } // EF

    private Customer(Guid id, string firstName, string lastName, string email,
        string? phoneNumber, ShippingAddress shippingAddress)
    {
        Id = id;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        PhoneNumber = phoneNumber;
        ShippingAddress = shippingAddress;
    }

    public static Result<Customer> Create(
        string? firstName,
        string? lastName,
        string? email,
        string? phoneNumber,
        ShippingAddress shippingAddress)
    {
        var fields = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(firstName) || firstName.Trim().Length is < 1 or > 100)
            fields["firstName"] = ["First name is required and must be 1-100 characters."];
        if (string.IsNullOrWhiteSpace(lastName) || lastName.Trim().Length is < 1 or > 100)
            fields["lastName"] = ["Last name is required and must be 1-100 characters."];
        if (string.IsNullOrWhiteSpace(email) || !EmailRegex.IsMatch(email.Trim()))
            fields["email"] = ["A valid email address is required."];

        var normalizedPhone = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
        if (normalizedPhone is not null && !PhoneRegex.IsMatch(normalizedPhone))
            fields["phoneNumber"] = ["Phone number is not valid."];

        if (shippingAddress is null)
            fields["shippingAddress"] = ["Shipping address is required."];

        if (fields.Count > 0)
            return Error.Validation(fields);

        return new Customer(
            Guid.NewGuid(),
            firstName!.Trim(),
            lastName!.Trim(),
            email!.Trim().ToLowerInvariant(),
            normalizedPhone,
            shippingAddress!);
    }
}
