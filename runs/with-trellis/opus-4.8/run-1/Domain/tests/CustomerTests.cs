namespace OrderManagement.Domain.Tests;

using OrderManagement.Domain;
using Trellis.Primitives;

public class CustomerTests
{
    [Fact]
    public void Create_WithValidData_NoPhone_Succeeds()
    {
        var customer = new Customer(
            TestData.FirstName(), TestData.LastName(), TestData.Email(), Maybe<PhoneNumber>.None, TestData.Address());

        customer.FirstName.Value.Should().Be("Jane");
        customer.Phone.Should().BeNone();
    }

    [Fact]
    public void Create_WithPhone_StoresPhone()
    {
        var customer = new Customer(
            TestData.FirstName(), TestData.LastName(), TestData.Email(),
            Maybe<PhoneNumber>.From(TestData.Phone()), TestData.Address());

        customer.Phone.Should().HaveValue();
    }

    [Fact]
    public void Email_WithInvalidFormat_FailsValidation()
    {
        var result = EmailAddress.TryCreate("not-an-email");
        result.Should().BeFailure();
    }

    [Fact]
    public void FirstName_WhenBlank_FailsValidation()
    {
        var result = FirstName.TryCreate("");
        result.Should().BeFailure();
    }
}
