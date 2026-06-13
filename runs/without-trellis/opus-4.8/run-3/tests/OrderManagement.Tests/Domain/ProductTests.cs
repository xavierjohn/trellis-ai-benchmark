using OrderManagement.Domain.Common;
using OrderManagement.Domain.Products;

namespace OrderManagement.Tests.Domain;

public class ProductTests
{
    [Fact]
    public void Create_WithValidData_Succeeds()
    {
        var result = Product.Create("Widget", "WIDGET01", 9.99m, 5);

        Assert.True(result.IsSuccess);
        Assert.Equal("WIDGET01", result.Value.Sku);
        Assert.Equal(5, result.Value.StockQuantity);
    }

    [Theory]
    [InlineData("ab")]            // too short
    [InlineData("lowercase01")]   // not uppercase
    [InlineData("HAS SPACE")]     // invalid char
    public void Create_WithInvalidSku_FailsValidation(string sku)
    {
        var result = Product.Create("Widget", sku, 9.99m);

        Assert.True(result.IsFailure);
        Assert.Contains("sku", result.Error!.FieldErrors!.Keys);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonPositivePrice_FailsValidation(decimal price)
    {
        var result = Product.Create("Widget", "WIDGET01", price);

        Assert.True(result.IsFailure);
        Assert.Contains("unitPrice", result.Error!.FieldErrors!.Keys);
    }

    [Fact]
    public void AddStock_IncreasesQuantity()
    {
        var product = Product.Create("Widget", "WIDGET01", 9.99m, 5).Value;

        var result = product.AddStock(10);

        Assert.True(result.IsSuccess);
        Assert.Equal(15, product.StockQuantity);
    }

    [Fact]
    public void AddStock_WithNonPositiveQuantity_Fails()
    {
        var product = Product.Create("Widget", "WIDGET01", 9.99m, 5).Value;

        var result = product.AddStock(0);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error!.Type);
    }

    [Fact]
    public void ReserveStock_WithSufficientStock_Succeeds()
    {
        var product = Product.Create("Widget", "WIDGET01", 9.99m, 5).Value;

        var result = product.ReserveStock(3);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, product.StockQuantity);
    }

    [Fact]
    public void ReserveStock_WithInsufficientStock_Fails()
    {
        var product = Product.Create("Widget", "WIDGET01", 9.99m, 5).Value;

        var result = product.ReserveStock(6);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        Assert.Equal(5, product.StockQuantity); // unchanged
    }

    [Fact]
    public void ReleaseStock_RestoresQuantity()
    {
        var product = Product.Create("Widget", "WIDGET01", 9.99m, 5).Value;
        product.ReserveStock(3);

        product.ReleaseStock(3);

        Assert.Equal(5, product.StockQuantity);
    }
}
