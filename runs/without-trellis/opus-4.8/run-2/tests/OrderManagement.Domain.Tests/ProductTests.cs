using OrderManagement.Domain.Common;
using OrderManagement.Domain.Products;
using Xunit;

namespace OrderManagement.Domain.Tests;

public class ProductTests
{
    [Fact]
    public void Create_with_valid_data_succeeds_with_zero_stock()
    {
        var result = Product.Create("Widget", "SKU123", 9.99m);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.StockQuantity);
        Assert.Equal("SKU123", result.Value.Sku);
    }

    [Theory]
    [InlineData("ab")]          // too short
    [InlineData("sku123")]      // lowercase
    [InlineData("SKU 123")]     // space
    [InlineData("SKU-123")]     // hyphen
    public void Create_with_invalid_sku_fails(string sku)
    {
        var result = Product.Create("Widget", sku, 9.99m);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Create_with_non_positive_price_fails(decimal price)
    {
        var result = Product.Create("Widget", "SKU123", price);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
    }

    [Fact]
    public void AddStock_increases_quantity()
    {
        var product = Product.Create("Widget", "SKU123", 5m).Value;

        var result = product.AddStock(10);

        Assert.True(result.IsSuccess);
        Assert.Equal(10, product.StockQuantity);
    }

    [Fact]
    public void AddStock_with_non_positive_quantity_fails()
    {
        var product = Product.Create("Widget", "SKU123", 5m).Value;

        var result = product.AddStock(0);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
    }

    [Fact]
    public void ReserveStock_decreases_quantity()
    {
        var product = TestData.ProductWithStock(stock: 10);

        var result = product.ReserveStock(4);

        Assert.True(result.IsSuccess);
        Assert.Equal(6, product.StockQuantity);
    }

    [Fact]
    public void ReserveStock_insufficient_fails_and_does_not_go_below_zero()
    {
        var product = TestData.ProductWithStock(stock: 3);

        var result = product.ReserveStock(5);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
        Assert.Equal(3, product.StockQuantity);
    }

    [Fact]
    public void ReleaseStock_restores_quantity()
    {
        var product = TestData.ProductWithStock(stock: 5);
        product.ReserveStock(5);

        product.ReleaseStock(5);

        Assert.Equal(5, product.StockQuantity);
    }
}
