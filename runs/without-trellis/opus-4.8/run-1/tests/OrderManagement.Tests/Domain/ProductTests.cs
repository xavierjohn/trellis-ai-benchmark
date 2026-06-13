using OrderManagement.Domain.Common;
using OrderManagement.Domain.Products;

namespace OrderManagement.Tests.Domain;

public class ProductTests
{
    [Fact]
    public void Create_WithValidData_Succeeds()
    {
        var result = Product.Create("Widget", "WIDGET01", 9.99m);

        Assert.True(result.IsSuccess);
        Assert.Equal("Widget", result.Value.ProductName);
        Assert.Equal("WIDGET01", result.Value.Sku.Value);
        Assert.Equal(0, result.Value.StockQuantity);
    }

    [Theory]
    [InlineData("ab")]            // too short
    [InlineData("lowercase")]     // not uppercase
    [InlineData("HAS SPACE")]     // invalid char
    public void Create_WithInvalidSku_Fails(string sku)
    {
        var result = Product.Create("Widget", sku, 1m);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Create_WithNonPositivePrice_Fails()
    {
        var result = Product.Create("Widget", "WIDGET01", 0m);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void AddStock_IncreasesQuantity()
    {
        var product = Product.Create("Widget", "WIDGET01", 1m).Value;

        var result = product.AddStock(50);

        Assert.True(result.IsSuccess);
        Assert.Equal(50, product.StockQuantity);
    }

    [Fact]
    public void AddStock_WithNonPositive_Fails()
    {
        var product = Product.Create("Widget", "WIDGET01", 1m).Value;
        Assert.True(product.AddStock(0).IsFailure);
    }

    [Fact]
    public void ReserveStock_WhenSufficient_Decreases()
    {
        var product = Product.Create("Widget", "WIDGET01", 1m).Value;
        product.AddStock(10);

        var result = product.ReserveStock(4);

        Assert.True(result.IsSuccess);
        Assert.Equal(6, product.StockQuantity);
    }

    [Fact]
    public void ReserveStock_WhenInsufficient_FailsAndDoesNotGoNegative()
    {
        var product = Product.Create("Widget", "WIDGET01", 1m).Value;
        product.AddStock(3);

        var result = product.ReserveStock(5);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
        Assert.Equal(3, product.StockQuantity);
    }

    [Fact]
    public void ReleaseStock_RestoresQuantity()
    {
        var product = Product.Create("Widget", "WIDGET01", 1m).Value;
        product.AddStock(10);
        product.ReserveStock(4);

        product.ReleaseStock(4);

        Assert.Equal(10, product.StockQuantity);
    }
}
