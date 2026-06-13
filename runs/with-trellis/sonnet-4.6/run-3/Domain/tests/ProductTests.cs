namespace Domain.Tests;

using OrderManagement.Domain;
using Trellis.Testing;

public class ProductTests
{
    private static Product MakeProduct(int stock = 0)
    {
        var name = ProductName.TryCreate("Widget").Unwrap();
        var sku = Sku.TryCreate("WGT001").Unwrap();
        var price = UnitPrice.TryCreate(9.99m).Unwrap();
        var product = new Product(name, sku, price);
        if (stock > 0)
            product.AddStock(stock).Should().BeSuccess();

        return product;
    }

    [Fact]
    public void Create_valid_product_succeeds()
    {
        var product = MakeProduct();
        product.ProductName.Value.Should().Be("Widget");
        product.StockQuantity.Should().Be(0);
    }

    [Fact]
    public void AddStock_positive_quantity_increases_stock()
    {
        var product = MakeProduct();
        product.AddStock(10).Should().BeSuccess();
        product.StockQuantity.Should().Be(10);
    }

    [Fact]
    public void AddStock_zero_quantity_fails()
    {
        var product = MakeProduct();
        product.AddStock(0).Should().BeFailure();
    }

    [Fact]
    public void AddStock_negative_quantity_fails()
    {
        var product = MakeProduct();
        product.AddStock(-5).Should().BeFailure();
    }

    [Fact]
    public void ReserveStock_sufficient_stock_succeeds()
    {
        var product = MakeProduct(10);
        product.ReserveStock(5).Should().BeSuccess();
        product.StockQuantity.Should().Be(5);
    }

    [Fact]
    public void ReserveStock_insufficient_stock_fails()
    {
        var product = MakeProduct(3);
        product.ReserveStock(5).Should().BeFailure();
    }

    [Fact]
    public void ReserveStock_exact_quantity_succeeds()
    {
        var product = MakeProduct(5);
        product.ReserveStock(5).Should().BeSuccess();
        product.StockQuantity.Should().Be(0);
    }

    [Fact]
    public void ReleaseStock_restores_stock()
    {
        var product = MakeProduct(10);
        product.ReserveStock(5).Should().BeSuccess();
        product.ReleaseStock(5);
        product.StockQuantity.Should().Be(10);
    }

    [Fact]
    public void Create_product_with_invalid_sku_fails()
    {
        var result = Sku.TryCreate("a b");
        result.Should().BeFailure();
    }
}
