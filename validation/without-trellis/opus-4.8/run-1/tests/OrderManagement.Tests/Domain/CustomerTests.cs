using OrderManagement.Api.Domain.Common;
using OrderManagement.Api.Domain.Customers;
using OrderManagement.Tests.Support;
using Xunit;

namespace OrderManagement.Tests.Domain;

public class CustomerTests
{
    [Fact]
    public void Create_WithValidData_NoPhone_Succeeds()
    {
        var customer = Customer.Create("Jane", "Doe", "jane@example.com", null, TestData.ValidAddress());

        Assert.Equal("Jane", customer.FirstName);
        Assert.Equal("Doe", customer.LastName);
        Assert.Equal("jane@example.com", customer.Email);
        Assert.Null(customer.PhoneNumber);
        Assert.NotEqual(Guid.Empty, customer.Id);
    }

    [Fact]
    public void Create_WithValidPhone_Succeeds()
    {
        var customer = Customer.Create("Jane", "Doe", "jane@example.com", "+1 (555) 123-4567", TestData.ValidAddress());
        Assert.Equal("+1 (555) 123-4567", customer.PhoneNumber);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing@domain")]
    [InlineData("")]
    public void Create_WithInvalidEmail_Throws(string email)
    {
        var ex = Assert.Throws<ValidationAppException>(() =>
            Customer.Create("Jane", "Doe", email, null, TestData.ValidAddress()));
        Assert.Contains("email", ex.Errors.Keys);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankFirstName_Throws(string firstName)
    {
        var ex = Assert.Throws<ValidationAppException>(() =>
            Customer.Create(firstName, "Doe", "jane@example.com", null, TestData.ValidAddress()));
        Assert.Contains("firstName", ex.Errors.Keys);
    }

    [Fact]
    public void Create_WithInvalidPhone_Throws()
    {
        var ex = Assert.Throws<ValidationAppException>(() =>
            Customer.Create("Jane", "Doe", "jane@example.com", "abc", TestData.ValidAddress()));
        Assert.Contains("phoneNumber", ex.Errors.Keys);
    }

    [Fact]
    public void Create_WithIncompleteAddress_Throws()
    {
        var badAddress = new ShippingAddress("123 Main", "", "IL", "62701", "USA");
        var ex = Assert.Throws<ValidationAppException>(() =>
            Customer.Create("Jane", "Doe", "jane@example.com", null, badAddress));
        Assert.Contains("shippingAddress", ex.Errors.Keys);
    }
}
