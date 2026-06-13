using OrderManagement.Api.Domain;
using Xunit;

namespace OrderManagement.Tests.Domain;

public class CustomerDomainTests
{
    [Fact]
    public void Create_ValidCustomer_WithPhone_Succeeds()
    {
        var address = new ShippingAddress("123 Main St", "Springfield", "IL", "62701", "US");
        var customer = Customer.Create("John", "Doe", "john@example.com", "+1-555-0100", address);

        Assert.Equal("John", customer.FirstName);
        Assert.Equal("Doe", customer.LastName);
        Assert.Equal("john@example.com", customer.Email);
        Assert.Equal("+1-555-0100", customer.PhoneNumber);
        Assert.Equal("123 Main St", customer.ShippingAddress.Street);
        Assert.NotEqual(Guid.Empty, customer.Id);
    }

    [Fact]
    public void Create_ValidCustomer_WithoutPhone_Succeeds()
    {
        var address = new ShippingAddress("123 Main St", "Springfield", "IL", "62701", "US");
        var customer = Customer.Create("Jane", "Smith", "jane@example.com", null, address);

        Assert.Null(customer.PhoneNumber);
        Assert.Equal("Jane", customer.FirstName);
    }

    [Fact]
    public void ShippingAddress_AllFields_AreSet()
    {
        var address = new ShippingAddress("456 Elm St", "Chicago", "IL", "60601", "US");
        var customer = Customer.Create("Alice", "Jones", "alice@example.com", null, address);

        Assert.Equal("456 Elm St", customer.ShippingAddress.Street);
        Assert.Equal("Chicago", customer.ShippingAddress.City);
        Assert.Equal("IL", customer.ShippingAddress.State);
        Assert.Equal("60601", customer.ShippingAddress.PostalCode);
        Assert.Equal("US", customer.ShippingAddress.Country);
    }
}
