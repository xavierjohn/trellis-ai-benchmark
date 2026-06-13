using System.Text.RegularExpressions;
using OrderManagement.Domain.Common;

namespace OrderManagement.Domain.Customers;

/// <summary>Email value object with format validation.</summary>
public sealed partial class Email
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Result<Email> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Error.Validation(nameof(Email), "Email is required.");

        var trimmed = value.Trim();
        if (trimmed.Length > 254 || !EmailRegex().IsMatch(trimmed))
            return Error.Validation(nameof(Email), "Email is not a valid email address.");

        return new Email(trimmed.ToLowerInvariant());
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();
}
