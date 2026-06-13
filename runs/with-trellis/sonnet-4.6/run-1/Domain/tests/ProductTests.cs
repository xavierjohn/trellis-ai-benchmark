namespace Domain.Tests;

using OrderManagement.Domain;

public class ProductTests
{
    [Fact]
    public void AddStock_then_reserve_stock_updates_quantity()
    {
        var product = new Product(ProductName.Create("Widget"), Sku.Create("SKU123"), 12.5m);

        product.AddStock(10).Should().BeSuccess();
        product.ReserveStock(4).Should().BeSuccess();

        product.StockQuantity.Should().Be(6);
    }

    [Fact]
    public void ReserveStock_with_insufficient_stock_returns_invalid_input()
    {
        var product = new Product(ProductName.Create("Widget"), Sku.Create("SKU123"), 12.5m);

        var result = product.ReserveStock(1);

        result.Should().BeFailureOfType<Error.InvalidInput>();
    }
}
