namespace Domain.Tests;

using OrderManagement.Domain;
using Trellis.Testing;

public class CustomerTests
{
    private static FirstName MakeFirstName() => FirstName.TryCreate("John").Unwrap();
    private static LastName MakeLastName() => LastName.TryCreate("Doe").Unwrap();
    private static Email MakeEmail() => Email.TryCreate("john.doe@example.com").Unwrap();
    private static ShippingAddress MakeAddress() => ShippingAddress.TryCreate("123 Main St", "Springfield", "IL", "62701", "US").Unwrap();

    [Fact]
    public void Create_valid_customer_without_phone_succeeds()
    {
        var customer = new Customer(MakeFirstName(), MakeLastName(), MakeEmail(), Maybe<PhoneNumber>.None, MakeAddress());

        customer.Email.Value.Should().Be("john.doe@example.com");
        customer.PhoneNumber.Should().BeNone();
    }

    [Fact]
    public void Create_valid_customer_with_phone_succeeds()
    {
        var phone = PhoneNumber.TryCreate("555-1234").Unwrap();
        var customer = new Customer(MakeFirstName(), MakeLastName(), MakeEmail(), Maybe.From(phone), MakeAddress());

        customer.PhoneNumber.Should().HaveValue();
    }

    [Fact]
    public void Create_customer_with_invalid_email_fails()
    {
        var result = Email.TryCreate("not-an-email");
        result.Should().BeFailure();
    }

    [Fact]
    public void Create_customer_with_blank_first_name_fails()
    {
        var result = FirstName.TryCreate(string.Empty);
        result.Should().BeFailure();
    }

    [Fact]
    public void Create_customer_with_blank_last_name_fails()
    {
        var result = LastName.TryCreate(string.Empty);
        result.Should().BeFailure();
    }

    [Fact]
    public void Create_customer_with_missing_address_street_fails()
    {
        var result = ShippingAddress.TryCreate(string.Empty, "City", "State", "12345", "US");
        result.Should().BeFailure();
    }
}
