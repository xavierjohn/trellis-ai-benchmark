namespace Domain.Tests;

using OrderManagement.Domain;
using Trellis.Primitives;

public class CustomerTests
{
    [Fact]
    public void Create_with_valid_data_succeeds()
    {
        var customer = TestData.Customer();

        customer.FirstName.Value.Should().Be("Alice");
        customer.Email.Value.Should().Be("alice@example.com");
        customer.Phone.Should().BeNone();
    }

    [Fact]
    public void Create_with_phone_number_succeeds()
    {
        var phone = PhoneNumber.TryCreate("+15551234567").Unwrap();
        var customer = Customer.Create(
            FirstName.Create("Bob"),
            LastName.Create("Jones"),
            EmailAddress.Create("bob@example.com"),
            Maybe.From(phone),
            TestData.Address());

        customer.Phone.Should().HaveValue();
    }

    [Fact]
    public void Invalid_email_fails()
    {
        var result = EmailAddress.TryCreate("not-an-email");

        result.Should().BeFailure();
    }

    [Fact]
    public void Blank_first_name_fails()
    {
        var result = FirstName.TryCreate("");

        result.Should().BeFailure();
    }
}

public class ProductTests
{
    [Fact]
    public void Create_with_valid_data_succeeds()
    {
        var product = TestData.Product(stock: 0);

        product.Sku.Value.Should().Be("ABC123");
        product.StockQuantity.Value.Should().Be(0);
    }

    [Fact]
    public void AddStock_increases_quantity()
    {
        var product = TestData.Product(stock: 5);

        var result = product.AddStock(10);

        result.Should().BeSuccess();
        product.StockQuantity.Value.Should().Be(15);
    }

    [Fact]
    public void AddStock_with_non_positive_quantity_fails()
    {
        var product = TestData.Product(stock: 5);

        product.AddStock(0).Should().BeFailureOfType<Error.InvalidInput>();
    }

    [Fact]
    public void ReserveStock_decreases_quantity()
    {
        var product = TestData.Product(stock: 10);

        var result = product.ReserveStock(Quantity.Create(3));

        result.Should().BeSuccess();
        product.StockQuantity.Value.Should().Be(7);
    }

    [Fact]
    public void ReserveStock_with_insufficient_stock_fails()
    {
        var product = TestData.Product(stock: 2);

        product.ReserveStock(Quantity.Create(5)).Should().BeFailureOfType<Error.InvalidInput>();
        product.StockQuantity.Value.Should().Be(2);
    }

    [Fact]
    public void Invalid_sku_fails()
    {
        Sku.TryCreate("ab").Should().BeFailure();
        Sku.TryCreate("lower123").Should().BeFailure();
    }

    [Fact]
    public void Non_positive_unit_price_fails()
    {
        UnitPrice.TryCreate(0m).Should().BeFailure();
    }
}
