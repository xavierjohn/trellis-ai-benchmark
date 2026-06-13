using OrderManagement.Domain.Common;
using OrderManagement.Domain.Customers;

namespace OrderManagement.Tests.Domain;

public class CustomerTests
{
    private static ShippingAddress ValidAddress() =>
        ShippingAddress.Create("1 Main St", "Springfield", "IL", "62704", "US").Value;

    [Fact]
    public void Create_WithValidData_Succeeds()
    {
        var result = Customer.Create("Jane", "Doe", "jane@example.com", "+1 555 123 4567", ValidAddress());

        Assert.True(result.IsSuccess);
        Assert.Equal("Jane", result.Value.FirstName);
        Assert.Equal("jane@example.com", result.Value.Email);
        Assert.Equal("+1 555 123 4567", result.Value.PhoneNumber);
    }

    [Fact]
    public void Create_WithoutPhone_Succeeds()
    {
        var result = Customer.Create("Jane", "Doe", "jane@example.com", null, ValidAddress());

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.PhoneNumber);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing@domain")]
    [InlineData("")]
    public void Create_WithInvalidEmail_FailsValidation(string email)
    {
        var result = Customer.Create("Jane", "Doe", email, null, ValidAddress());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        Assert.Contains("email", result.Error.FieldErrors!.Keys);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankFirstName_FailsValidation(string firstName)
    {
        var result = Customer.Create(firstName, "Doe", "jane@example.com", null, ValidAddress());

        Assert.True(result.IsFailure);
        Assert.Contains("firstName", result.Error!.FieldErrors!.Keys);
    }

    [Fact]
    public void Create_WithInvalidPhone_FailsValidation()
    {
        var result = Customer.Create("Jane", "Doe", "jane@example.com", "abc", ValidAddress());

        Assert.True(result.IsFailure);
        Assert.Contains("phoneNumber", result.Error!.FieldErrors!.Keys);
    }

    [Fact]
    public void ShippingAddress_WithMissingFields_FailsValidation()
    {
        var result = ShippingAddress.Create("1 Main", "", "IL", "62704", "");

        Assert.True(result.IsFailure);
        Assert.Contains("city", result.Error!.FieldErrors!.Keys);
        Assert.Contains("country", result.Error.FieldErrors!.Keys);
    }
}
