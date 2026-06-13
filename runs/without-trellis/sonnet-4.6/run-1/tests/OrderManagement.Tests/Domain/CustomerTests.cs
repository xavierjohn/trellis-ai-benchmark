using OrderManagement.Domain.Aggregates;
using OrderManagement.Domain.Exceptions;
using OrderManagement.Domain.ValueObjects;
using Xunit;

namespace OrderManagement.Tests.Domain;

public class CustomerTests
{
    private static ShippingAddress ValidAddress() =>
        new("123 Main St", "Springfield", "IL", "62701", "USA");

    [Fact]
    public void Create_WithValidData_Succeeds()
    {
        var customer = Customer.Create("John", "Doe", "john@example.com", null, ValidAddress());

        Assert.Equal("John", customer.FirstName);
        Assert.Equal("Doe", customer.LastName);
        Assert.Equal("john@example.com", customer.Email);
        Assert.Null(customer.PhoneNumber);
        Assert.NotEqual(Guid.Empty, customer.Id);
    }

    [Fact]
    public void Create_WithPhone_Succeeds()
    {
        var customer = Customer.Create("Jane", "Smith", "jane@example.com", "+1-555-0100", ValidAddress());
        Assert.Equal("+1-555-0100", customer.PhoneNumber);
    }

    [Fact]
    public void Create_WithBlankFirstName_ThrowsValidation()
    {
        var ex = Assert.Throws<ValidationException>(() =>
            Customer.Create("", "Doe", "john@example.com", null, ValidAddress()));
        Assert.Contains(ex.Errors, e => e.Contains("FirstName"));
    }

    [Fact]
    public void Create_WithBlankLastName_ThrowsValidation()
    {
        var ex = Assert.Throws<ValidationException>(() =>
            Customer.Create("John", "", "john@example.com", null, ValidAddress()));
        Assert.Contains(ex.Errors, e => e.Contains("LastName"));
    }

    [Fact]
    public void Create_WithInvalidEmail_ThrowsValidation()
    {
        var ex = Assert.Throws<ValidationException>(() =>
            Customer.Create("John", "Doe", "not-an-email", null, ValidAddress()));
        Assert.Contains(ex.Errors, e => e.Contains("Email"));
    }

    [Fact]
    public void Create_WithInvalidPhone_ThrowsValidation()
    {
        var ex = Assert.Throws<ValidationException>(() =>
            Customer.Create("John", "Doe", "john@example.com", "abc", ValidAddress()));
        Assert.Contains(ex.Errors, e => e.Contains("PhoneNumber"));
    }

    [Fact]
    public void Create_WithMissingAddressFields_ThrowsValidation()
    {
        var address = new ShippingAddress("", "Springfield", "IL", "62701", "USA");
        var ex = Assert.Throws<ValidationException>(() =>
            Customer.Create("John", "Doe", "john@example.com", null, address));
        Assert.Contains(ex.Errors, e => e.Contains("Street"));
    }

    [Fact]
    public void Create_WithLongFirstName_ThrowsValidation()
    {
        var longName = new string('A', 101);
        var ex = Assert.Throws<ValidationException>(() =>
            Customer.Create(longName, "Doe", "john@example.com", null, ValidAddress()));
        Assert.Contains(ex.Errors, e => e.Contains("FirstName"));
    }
}
