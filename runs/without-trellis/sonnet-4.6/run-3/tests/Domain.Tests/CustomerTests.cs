using Domain.Customers;

namespace Domain.Tests;

public class CustomerTests
{
    private static ShippingAddress ValidAddress() =>
        ShippingAddress.Create("123 Main St", "Springfield", "IL", "62701", "US").Value!;

    [Fact]
    public void Create_ValidCustomerWithPhone_Succeeds()
    {
        var result = Customer.Create("John", "Doe", "john@example.com", "+1234567890", ValidAddress());
        Assert.True(result.IsSuccess);
        Assert.Equal("john@example.com", result.Value!.Email);
    }

    [Fact]
    public void Create_ValidCustomerWithoutPhone_Succeeds()
    {
        var result = Customer.Create("Jane", "Doe", "jane@example.com", null, ValidAddress());
        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.PhoneNumber);
    }

    [Fact]
    public void Create_InvalidEmail_Fails()
    {
        var result = Customer.Create("John", "Doe", "not-an-email", null, ValidAddress());
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Create_BlankFirstName_Fails()
    {
        var result = Customer.Create("", "Doe", "john@example.com", null, ValidAddress());
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Create_BlankLastName_Fails()
    {
        var result = Customer.Create("John", "", "john@example.com", null, ValidAddress());
        Assert.False(result.IsSuccess);
    }
}
