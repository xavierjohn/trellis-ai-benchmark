using System.Text.RegularExpressions;
using OrderManagement.Domain.Common;

namespace OrderManagement.Domain.Customers;

/// <summary>Customer aggregate root.</summary>
public sealed partial class Customer
{
    public Guid Id { get; private set; }
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public Email Email { get; private set; } = null!;
    public string? PhoneNumber { get; private set; }
    public ShippingAddress ShippingAddress { get; private set; } = null!;

    private Customer() { } // EF

    private Customer(Guid id, string firstName, string lastName, Email email,
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

        if (string.IsNullOrWhiteSpace(firstName) || firstName.Trim().Length > 100)
            fields[nameof(FirstName)] = new[] { "FirstName is required and must be 1–100 characters." };

        if (string.IsNullOrWhiteSpace(lastName) || lastName.Trim().Length > 100)
            fields[nameof(LastName)] = new[] { "LastName is required and must be 1–100 characters." };

        var emailResult = Email.Create(email);
        if (emailResult.IsFailure)
            fields[nameof(Email)] = new[] { emailResult.Error!.Message };

        string? normalizedPhone = null;
        if (!string.IsNullOrWhiteSpace(phoneNumber))
        {
            normalizedPhone = phoneNumber.Trim();
            if (!PhoneRegex().IsMatch(normalizedPhone))
                fields[nameof(PhoneNumber)] = new[] { "PhoneNumber is not a valid phone number." };
        }

        if (fields.Count > 0)
            return Error.Validation("Customer is invalid.", fields);

        return new Customer(
            Guid.NewGuid(),
            firstName!.Trim(),
            lastName!.Trim(),
            emailResult.Value,
            normalizedPhone,
            shippingAddress);
    }

    [GeneratedRegex(@"^\+?[0-9\s\-\(\)]{7,20}$", RegexOptions.CultureInvariant)]
    private static partial Regex PhoneRegex();
}
