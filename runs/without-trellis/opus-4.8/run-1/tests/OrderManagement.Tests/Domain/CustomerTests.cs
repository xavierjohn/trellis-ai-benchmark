using OrderManagement.Domain.Common;
using OrderManagement.Domain.Customers;

namespace OrderManagement.Tests.Domain;

public class CustomerTests
{
    private static ShippingAddress ValidAddress() =>
        ShippingAddress.Create("1 Main St", "Springfield", "IL", "62704", "USA").Value;

    [Fact]
    public void Create_WithValidData_Succeeds()
    {
        var result = Customer.Create("Jane", "Doe", "jane@example.com", null, ValidAddress());

        Assert.True(result.IsSuccess);
        Assert.Equal("Jane", result.Value.FirstName);
        Assert.Equal("jane@example.com", result.Value.Email.Value);
        Assert.Null(result.Value.PhoneNumber);
    }

    [Fact]
    public void Create_WithPhone_Succeeds()
    {
        var result = Customer.Create("Jane", "Doe", "jane@example.com", "+1 (555) 123-4567", ValidAddress());

        Assert.True(result.IsSuccess);
        Assert.Equal("+1 (555) 123-4567", result.Value.PhoneNumber);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing@domain")]
    [InlineData("")]
    public void Create_WithInvalidEmail_Fails(string email)
    {
        var result = Customer.Create("Jane", "Doe", email, null, ValidAddress());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
    }

    [Fact]
    public void Create_WithBlankName_Fails()
    {
        var result = Customer.Create("  ", "Doe", "jane@example.com", null, ValidAddress());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
    }

    [Fact]
    public void ShippingAddress_WithMissingField_Fails()
    {
        var result = ShippingAddress.Create("1 Main St", "", "IL", "62704", "USA");

        Assert.True(result.IsFailure);
    }
}
