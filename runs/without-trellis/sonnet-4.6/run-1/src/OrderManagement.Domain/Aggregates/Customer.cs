using OrderManagement.Domain.Exceptions;
using OrderManagement.Domain.ValueObjects;
using System.Text.RegularExpressions;

namespace OrderManagement.Domain.Aggregates;

public class Customer
{
    public Guid Id { get; private set; }
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string? PhoneNumber { get; private set; }
    public ShippingAddress ShippingAddress { get; private set; } = default!;

    private Customer() { }

    public static Customer Create(
        string firstName,
        string lastName,
        string email,
        string? phoneNumber,
        ShippingAddress shippingAddress)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(firstName) || firstName.Length > 100)
            errors.Add("FirstName must be between 1 and 100 characters.");
        if (string.IsNullOrWhiteSpace(lastName) || lastName.Length > 100)
            errors.Add("LastName must be between 1 and 100 characters.");
        if (!IsValidEmail(email))
            errors.Add("Email must be a valid email address.");
        if (phoneNumber is not null && !IsValidPhone(phoneNumber))
            errors.Add("PhoneNumber is not a valid phone number.");

        ValidateShippingAddress(shippingAddress, errors);

        if (errors.Count > 0)
            throw new ValidationException(errors);

        return new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            PhoneNumber = phoneNumber?.Trim(),
            ShippingAddress = shippingAddress
        };
    }

    private static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        return Regex.IsMatch(email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    private static bool IsValidPhone(string phone)
    {
        return Regex.IsMatch(phone.Trim(), @"^\+?[\d\s\-().]{7,20}$");
    }

    private static void ValidateShippingAddress(ShippingAddress? address, List<string> errors)
    {
        if (address is null)
        {
            errors.Add("ShippingAddress is required.");
            return;
        }
        if (string.IsNullOrWhiteSpace(address.Street))
            errors.Add("ShippingAddress.Street is required.");
        if (string.IsNullOrWhiteSpace(address.City))
            errors.Add("ShippingAddress.City is required.");
        if (string.IsNullOrWhiteSpace(address.State))
            errors.Add("ShippingAddress.State is required.");
        if (string.IsNullOrWhiteSpace(address.PostalCode))
            errors.Add("ShippingAddress.PostalCode is required.");
        if (string.IsNullOrWhiteSpace(address.Country))
            errors.Add("ShippingAddress.Country is required.");
    }
}
