using System.Text.RegularExpressions;
using OrderManagement.Api.Domain.Common;

namespace OrderManagement.Api.Domain.Customers;

/// <summary>Customer aggregate root.</summary>
public sealed class Customer
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    private static readonly Regex PhoneRegex = new(
        @"^\+?[0-9\s\-\(\)]{7,20}$", RegexOptions.Compiled);

    private Customer() { } // EF

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

    public Guid Id { get; private set; }
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string? PhoneNumber { get; private set; }
    public ShippingAddress ShippingAddress { get; private set; } = default!;

    public static Customer Create(
        string firstName,
        string lastName,
        string email,
        string? phoneNumber,
        ShippingAddress shippingAddress)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(firstName) || firstName.Length > 100)
            errors[nameof(firstName)] = ["FirstName is required and must be 1-100 characters."];

        if (string.IsNullOrWhiteSpace(lastName) || lastName.Length > 100)
            errors[nameof(lastName)] = ["LastName is required and must be 1-100 characters."];

        var normalizedEmail = email?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedEmail) || !EmailRegex.IsMatch(normalizedEmail))
            errors[nameof(email)] = ["A valid email address is required."];

        if (!string.IsNullOrWhiteSpace(phoneNumber) && !PhoneRegex.IsMatch(phoneNumber))
            errors[nameof(phoneNumber)] = ["PhoneNumber, when provided, must be a valid phone number."];

        ValidateAddress(shippingAddress, errors);

        if (errors.Count > 0)
            throw new ValidationAppException("Customer validation failed.", errors);

        var phone = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();

        return new Customer(
            Guid.NewGuid(),
            firstName.Trim(),
            lastName.Trim(),
            normalizedEmail,
            phone,
            shippingAddress);
    }

    private static void ValidateAddress(ShippingAddress? address, Dictionary<string, string[]> errors)
    {
        if (address is null)
        {
            errors["shippingAddress"] = ["Shipping address is required."];
            return;
        }

        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(address.Street)) missing.Add("street");
        if (string.IsNullOrWhiteSpace(address.City)) missing.Add("city");
        if (string.IsNullOrWhiteSpace(address.State)) missing.Add("state");
        if (string.IsNullOrWhiteSpace(address.PostalCode)) missing.Add("postalCode");
        if (string.IsNullOrWhiteSpace(address.Country)) missing.Add("country");

        if (missing.Count > 0)
            errors["shippingAddress"] = [$"Shipping address fields are required: {string.Join(", ", missing)}."];
    }
}
