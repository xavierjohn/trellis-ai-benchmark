using Domain.Products;

namespace Domain.Tests;

public class ProductTests
{
    [Fact]
    public void Create_ValidProduct_Succeeds()
    {
        var result = Product.Create("Widget", "WDG001", 9.99m);
        Assert.True(result.IsSuccess);
        Assert.Equal("WDG001", result.Value!.SKU);
    }

    [Fact]
    public void AddStock_PositiveQty_Succeeds()
    {
        var product = Product.Create("Widget", "WDG001", 9.99m, 10).Value!;
        var result = product.AddStock(5);
        Assert.True(result.IsSuccess);
        Assert.Equal(15, product.StockQuantity);
    }

    [Fact]
    public void AddStock_ZeroQty_Fails()
    {
        var product = Product.Create("Widget", "WDG001", 9.99m).Value!;
        var result = product.AddStock(0);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ReserveStock_SufficientQty_Succeeds()
    {
        var product = Product.Create("Widget", "WDG001", 9.99m, 10).Value!;
        var result = product.ReserveStock(5);
        Assert.True(result.IsSuccess);
        Assert.Equal(5, product.StockQuantity);
    }

    [Fact]
    public void ReserveStock_InsufficientQty_Fails()
    {
        var product = Product.Create("Widget", "WDG001", 9.99m, 3).Value!;
        var result = product.ReserveStock(5);
        Assert.False(result.IsSuccess);
    }
}
