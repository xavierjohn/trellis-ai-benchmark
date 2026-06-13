using OrderManagement.Api.Domain;
using Xunit;

namespace OrderManagement.Tests.Domain;

public class ProductDomainTests
{
    [Fact]
    public void Create_ValidProduct_Succeeds()
    {
        var product = Product.Create("Widget", "WGT001", 9.99m);

        Assert.Equal("Widget", product.ProductName);
        Assert.Equal("WGT001", product.SKU);
        Assert.Equal(9.99m, product.UnitPrice);
        Assert.Equal(0, product.StockQuantity);
        Assert.NotEqual(Guid.Empty, product.Id);
    }

    [Fact]
    public void AddStock_PositiveQuantity_IncreasesStock()
    {
        var product = Product.Create("Widget", "WGT001", 9.99m);

        product.AddStock(50);

        Assert.Equal(50, product.StockQuantity);
    }

    [Fact]
    public void AddStock_MultipleAdds_AccumulatesCorrectly()
    {
        var product = Product.Create("Widget", "WGT001", 9.99m);
        product.AddStock(30);
        product.AddStock(20);

        Assert.Equal(50, product.StockQuantity);
    }

    [Fact]
    public void AddStock_ZeroQuantity_ThrowsValidationException()
    {
        var product = Product.Create("Widget", "WGT001", 9.99m);

        Assert.Throws<DomainValidationException>(() => product.AddStock(0));
    }

    [Fact]
    public void AddStock_NegativeQuantity_ThrowsValidationException()
    {
        var product = Product.Create("Widget", "WGT001", 9.99m);

        Assert.Throws<DomainValidationException>(() => product.AddStock(-5));
    }

    [Fact]
    public void ReserveStock_SufficientStock_DecreasesStock()
    {
        var product = Product.Create("Widget", "WGT001", 9.99m);
        product.AddStock(100);

        product.ReserveStock(30);

        Assert.Equal(70, product.StockQuantity);
    }

    [Fact]
    public void ReserveStock_ExactStock_DecreasesToZero()
    {
        var product = Product.Create("Widget", "WGT001", 9.99m);
        product.AddStock(10);

        product.ReserveStock(10);

        Assert.Equal(0, product.StockQuantity);
    }

    [Fact]
    public void ReserveStock_InsufficientStock_ThrowsValidationException()
    {
        var product = Product.Create("Widget", "WGT001", 9.99m);
        product.AddStock(5);

        var ex = Assert.Throws<DomainValidationException>(() => product.ReserveStock(10));

        Assert.Contains("Insufficient stock", ex.Message);
    }

    [Fact]
    public void ReserveStock_ZeroStock_ThrowsValidationException()
    {
        var product = Product.Create("Widget", "WGT001", 9.99m);

        Assert.Throws<DomainValidationException>(() => product.ReserveStock(1));
    }

    [Fact]
    public void ReleaseStock_RestoresQuantity()
    {
        var product = Product.Create("Widget", "WGT001", 9.99m);
        product.AddStock(100);
        product.ReserveStock(30);

        product.ReleaseStock(30);

        Assert.Equal(100, product.StockQuantity);
    }
}
