using OrderManagement.Domain.Common;
using OrderManagement.Domain.Customers;
using Xunit;

namespace OrderManagement.Domain.Tests;

public class CustomerTests
{
    [Fact]
    public void Create_with_valid_data_and_phone_succeeds()
    {
        var result = Customer.Create("Jane", "Doe", "jane@example.com", "+1 555 1234", TestData.Address());

        Assert.True(result.IsSuccess);
        Assert.Equal("jane@example.com", result.Value.Email);
        Assert.Equal("+1 555 1234", result.Value.PhoneNumber);
    }

    [Fact]
    public void Create_without_phone_succeeds()
    {
        var result = Customer.Create("Jane", "Doe", "jane@example.com", null, TestData.Address());

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.PhoneNumber);
    }

    [Fact]
    public void Create_normalizes_email_to_lowercase()
    {
        var result = Customer.Create("Jane", "Doe", "Jane@Example.COM", null, TestData.Address());

        Assert.True(result.IsSuccess);
        Assert.Equal("jane@example.com", result.Value.Email);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing@domain")]
    [InlineData("")]
    public void Create_with_invalid_email_fails_validation(string email)
    {
        var result = Customer.Create("Jane", "Doe", email, null, TestData.Address());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
    }

    [Fact]
    public void Create_with_blank_name_fails_validation()
    {
        var result = Customer.Create("", "  ", "jane@example.com", null, TestData.Address());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
        Assert.NotNull(result.Error.Fields);
        Assert.Contains("firstName", result.Error.Fields!.Keys);
        Assert.Contains("lastName", result.Error.Fields!.Keys);
    }

    [Fact]
    public void Create_with_invalid_phone_fails_validation()
    {
        var result = Customer.Create("Jane", "Doe", "jane@example.com", "abc", TestData.Address());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
    }

    [Fact]
    public void Address_with_missing_field_fails_validation()
    {
        var result = ShippingAddress.Create("1 Main", "Town", "CA", "90001", "");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
    }
}
