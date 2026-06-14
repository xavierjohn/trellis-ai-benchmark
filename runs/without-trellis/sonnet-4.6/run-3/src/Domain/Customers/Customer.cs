using Domain.Common;
using System.Text.RegularExpressions;

namespace Domain.Customers;

public class Customer
{
    public Guid CustomerId { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }
    public ShippingAddress ShippingAddress { get; private set; } = null!;

    private Customer()
    {
    }

    public static Result<Customer> Create(
        string firstName,
        string lastName,
        string email,
        string? phoneNumber,
        ShippingAddress shippingAddress)
    {
        var errors = new List<string>();
        var normalizedEmail = email?.Trim().ToLowerInvariant() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(firstName) || firstName.Length > 100)
        {
            errors.Add("FirstName must be 1-100 characters.");
        }

        if (string.IsNullOrWhiteSpace(lastName) || lastName.Length > 100)
        {
            errors.Add("LastName must be 1-100 characters.");
        }

        if (!IsValidEmail(normalizedEmail))
        {
            errors.Add("Email must be a valid email address.");
        }

        if (phoneNumber != null && !IsValidPhone(phoneNumber))
        {
            errors.Add("PhoneNumber is not valid.");
        }

        if (shippingAddress == null)
        {
            errors.Add("ShippingAddress is required.");
        }

        if (errors.Count > 0)
        {
            return Result<Customer>.Failure(string.Join(" ", errors));
        }

        return Result<Customer>.Success(new Customer
        {
            CustomerId = Guid.NewGuid(),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Email = normalizedEmail,
            PhoneNumber = phoneNumber?.Trim(),
            ShippingAddress = shippingAddress!
        });
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
    }

    private static bool IsValidPhone(string phone)
    {
        return Regex.IsMatch(phone, @"^\+?[\d\s\-\(\)]{7,20}$");
    }
}

public class ShippingAddress
{
    public string Street { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string State { get; private set; } = string.Empty;
    public string PostalCode { get; private set; } = string.Empty;
    public string Country { get; private set; } = string.Empty;

    private ShippingAddress()
    {
    }

    public static Result<ShippingAddress> Create(string street, string city, string state, string postalCode, string country)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(street))
        {
            errors.Add("Street is required.");
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            errors.Add("City is required.");
        }

        if (string.IsNullOrWhiteSpace(state))
        {
            errors.Add("State is required.");
        }

        if (string.IsNullOrWhiteSpace(postalCode))
        {
            errors.Add("PostalCode is required.");
        }

        if (string.IsNullOrWhiteSpace(country))
        {
            errors.Add("Country is required.");
        }

        if (errors.Count > 0)
        {
            return Result<ShippingAddress>.Failure(string.Join(" ", errors));
        }

        return Result<ShippingAddress>.Success(new ShippingAddress
        {
            Street = street.Trim(),
            City = city.Trim(),
            State = state.Trim(),
            PostalCode = postalCode.Trim(),
            Country = country.Trim()
        });
    }
}
