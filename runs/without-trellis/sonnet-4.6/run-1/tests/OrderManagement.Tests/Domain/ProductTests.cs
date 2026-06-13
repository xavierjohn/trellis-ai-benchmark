using OrderManagement.Domain.Aggregates;
using OrderManagement.Domain.Exceptions;
using Xunit;

namespace OrderManagement.Tests.Domain;

public class ProductTests
{
    [Fact]
    public void Create_WithValidData_Succeeds()
    {
        var product = Product.Create("Widget", "WDG001", 9.99m);

        Assert.Equal("Widget", product.ProductName);
        Assert.Equal("WDG001", product.SKU);
        Assert.Equal(9.99m, product.UnitPrice);
        Assert.Equal(0, product.StockQuantity);
    }

    [Fact]
    public void Create_WithLowercaseSku_NormalizesToUppercase()
    {
        var product = Product.Create("Widget", "wdg001", 9.99m);
        Assert.Equal("WDG001", product.SKU);
    }

    [Fact]
    public void Create_WithBlankName_ThrowsValidation()
    {
        Assert.Throws<ValidationException>(() => Product.Create("", "WDG001", 9.99m));
    }

    [Fact]
    public void Create_WithInvalidSku_ThrowsValidation()
    {
        // Too short
        Assert.Throws<ValidationException>(() => Product.Create("Widget", "WD", 9.99m));
        // Contains lowercase (after trim) - but we uppercase, so test with special chars
        Assert.Throws<ValidationException>(() => Product.Create("Widget", "WDG-001", 9.99m));
        // Too long
        Assert.Throws<ValidationException>(() => Product.Create("Widget", new string('A', 21), 9.99m));
    }

    [Fact]
    public void Create_WithZeroPrice_ThrowsValidation()
    {
        Assert.Throws<ValidationException>(() => Product.Create("Widget", "WDG001", 0m));
    }

    [Fact]
    public void Create_WithNegativePrice_ThrowsValidation()
    {
        Assert.Throws<ValidationException>(() => Product.Create("Widget", "WDG001", -1m));
    }

    [Fact]
    public void AddStock_IncreasesQuantity()
    {
        var product = Product.Create("Widget", "WDG001", 9.99m);
        product.AddStock(100);
        Assert.Equal(100, product.StockQuantity);
    }

    [Fact]
    public void AddStock_WithZeroQuantity_ThrowsValidation()
    {
        var product = Product.Create("Widget", "WDG001", 9.99m);
        Assert.Throws<ValidationException>(() => product.AddStock(0));
    }

    [Fact]
    public void AddStock_WithNegativeQuantity_ThrowsValidation()
    {
        var product = Product.Create("Widget", "WDG001", 9.99m);
        Assert.Throws<ValidationException>(() => product.AddStock(-5));
    }

    [Fact]
    public void ReserveStock_DecreasesQuantity()
    {
        var product = Product.Create("Widget", "WDG001", 9.99m);
        product.AddStock(50);
        product.ReserveStock(20);
        Assert.Equal(30, product.StockQuantity);
    }

    [Fact]
    public void ReserveStock_WithInsufficientStock_ThrowsValidation()
    {
        var product = Product.Create("Widget", "WDG001", 9.99m);
        product.AddStock(5);
        var ex = Assert.Throws<ValidationException>(() => product.ReserveStock(10));
        Assert.Contains("Insufficient stock", ex.Message);
    }

    [Fact]
    public void ReserveStock_ExactAvailable_Succeeds()
    {
        var product = Product.Create("Widget", "WDG001", 9.99m);
        product.AddStock(10);
        product.ReserveStock(10);
        Assert.Equal(0, product.StockQuantity);
    }
}
