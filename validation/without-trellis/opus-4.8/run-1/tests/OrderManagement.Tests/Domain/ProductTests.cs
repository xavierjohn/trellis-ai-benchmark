using OrderManagement.Api.Domain.Common;
using OrderManagement.Api.Domain.Products;
using Xunit;

namespace OrderManagement.Tests.Domain;

public class ProductTests
{
    [Fact]
    public void Create_WithValidData_Succeeds()
    {
        var product = Product.Create("Widget", "WIDGET01", 9.99m, 5);

        Assert.Equal("Widget", product.ProductName);
        Assert.Equal("WIDGET01", product.Sku);
        Assert.Equal(9.99m, product.UnitPrice);
        Assert.Equal(5, product.StockQuantity);
    }

    [Theory]
    [InlineData("ab")]            // too short
    [InlineData("lowercase")]     // not uppercase
    [InlineData("HAS SPACE")]     // invalid char
    public void Create_WithInvalidSku_Throws(string sku)
    {
        Assert.Throws<ValidationAppException>(() => Product.Create("Widget", sku, 9.99m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonPositivePrice_Throws(decimal price)
    {
        Assert.Throws<ValidationAppException>(() => Product.Create("Widget", "WIDGET01", price));
    }

    [Fact]
    public void AddStock_IncreasesQuantity()
    {
        var product = Product.Create("Widget", "WIDGET01", 9.99m, 5);
        product.AddStock(10);
        Assert.Equal(15, product.StockQuantity);
    }

    [Fact]
    public void AddStock_WithNonPositive_Throws()
    {
        var product = Product.Create("Widget", "WIDGET01", 9.99m, 5);
        Assert.Throws<ValidationAppException>(() => product.AddStock(0));
    }

    [Fact]
    public void ReserveStock_DecreasesQuantity()
    {
        var product = Product.Create("Widget", "WIDGET01", 9.99m, 5);
        product.ReserveStock(3);
        Assert.Equal(2, product.StockQuantity);
    }

    [Fact]
    public void ReserveStock_WithInsufficientStock_Throws()
    {
        var product = Product.Create("Widget", "WIDGET01", 9.99m, 5);
        var ex = Assert.Throws<ValidationAppException>(() => product.ReserveStock(6));
        Assert.Contains("Insufficient stock", ex.Message);
        Assert.Equal(5, product.StockQuantity); // unchanged
    }

    [Fact]
    public void ReleaseStock_RestoresQuantity()
    {
        var product = Product.Create("Widget", "WIDGET01", 9.99m, 5);
        product.ReserveStock(3);
        product.ReleaseStock(3);
        Assert.Equal(5, product.StockQuantity);
    }
}
