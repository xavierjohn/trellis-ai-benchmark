namespace Domain.Tests;

using OrderManagement.Domain;
using Trellis.Primitives;

public class CustomerTests
{
    [Fact]
    public void Create_with_valid_data_succeeds()
    {
        var customer = Build.Customer();

        customer.FirstName.Value.Should().Be("Jane");
        customer.Email.Value.Should().Be("jane@example.com");
        customer.PhoneNumber.Should().BeNone();
        customer.ShippingAddress.City.Should().Be("Springfield");
    }

    [Fact]
    public void Create_with_phone_number_succeeds()
    {
        var customer = Build.Customer(phone: "+14155552671");

        customer.PhoneNumber.Should().HaveValue();
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("")]
    public void Invalid_email_fails_validation(string email)
    {
        var result = EmailAddress.TryCreate(email, "email");

        result.Should().BeFailure();
    }

    [Fact]
    public void Blank_first_name_fails_validation()
    {
        var result = FirstName.TryCreate("", "firstName");

        result.Should().BeFailure();
    }

    [Fact]
    public void Address_missing_fields_fails_validation()
    {
        var result = ShippingAddress.TryCreate("1 Main", null, "IL", "62701", "USA");

        result.Should().BeFailureOfType<Error.InvalidInput>();
    }
}
