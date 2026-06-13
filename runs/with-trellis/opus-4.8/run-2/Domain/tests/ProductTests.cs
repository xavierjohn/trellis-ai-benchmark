namespace Domain.Tests;

using OrderManagement.Domain;
using Trellis.Primitives;

public class ProductTests
{
    [Fact]
    public void Create_with_valid_data_succeeds()
    {
        var result = Product.TryCreate(
            ProductName.TryCreate("Widget").Unwrap(),
            Sku.TryCreate("WIDGET01").Unwrap(),
            MonetaryAmount.Create(9.99m));

        result.Should().BeSuccess();
        result.Unwrap().StockQuantity.Should().Be(0);
    }

    [Fact]
    public void Create_with_zero_price_fails()
    {
        var result = Product.TryCreate(
            ProductName.TryCreate("Widget").Unwrap(),
            Sku.TryCreate("WIDGET01").Unwrap(),
            MonetaryAmount.Create(0m));

        result.Should().BeFailureOfType<Error.InvalidInput>();
    }

    [Theory]
    [InlineData("ab")]          // too short
    [InlineData("lowercase")]   // not uppercase
    [InlineData("WITH SPACE")]  // invalid char
    public void Invalid_sku_fails_validation(string sku)
    {
        var result = Sku.TryCreate(sku, "sku");

        result.Should().BeFailure();
    }

    [Fact]
    public void AddStock_increases_quantity()
    {
        var product = Build.Product(stock: 5);

        product.AddStock(StockAddition.TryCreate(10).Unwrap()).Unwrap();

        product.StockQuantity.Should().Be(15);
    }

    [Fact]
    public void ReserveStock_decreases_quantity()
    {
        var product = Build.Product(stock: 10);

        var result = product.ReserveStock(4);

        result.Should().BeSuccess();
        product.StockQuantity.Should().Be(6);
    }

    [Fact]
    public void ReserveStock_with_insufficient_stock_fails_and_does_not_change_quantity()
    {
        var product = Build.Product(stock: 3);

        var result = product.ReserveStock(5);

        result.Should().BeFailureOfType<Error.InvalidInput>();
        product.StockQuantity.Should().Be(3);
    }

    [Fact]
    public void ReleaseStock_restores_quantity()
    {
        var product = Build.Product(stock: 2);

        product.ReleaseStock(3).Unwrap();

        product.StockQuantity.Should().Be(5);
    }
}
