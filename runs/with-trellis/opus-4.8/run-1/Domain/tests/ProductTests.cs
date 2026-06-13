namespace OrderManagement.Domain.Tests;

public class ProductTests
{
    [Fact]
    public void Create_WithValidData_StartsWithZeroStock()
    {
        var product = TestData.Product();
        product.StockQuantity.Should().Be(0);
    }

    [Fact]
    public void AddStock_WithPositiveQuantity_IncreasesStock()
    {
        var product = TestData.Product();
        product.AddStock(50).Should().BeSuccess();
        product.StockQuantity.Should().Be(50);
    }

    [Fact]
    public void AddStock_WithNonPositiveQuantity_Fails()
    {
        var product = TestData.Product();
        product.AddStock(0).Should().BeFailureOfType<Error.InvalidInput>();
    }

    [Fact]
    public void ReserveStock_WithSufficientStock_DecreasesStock()
    {
        var product = TestData.Product();
        product.AddStock(10).Discard();
        product.ReserveStock(4).Should().BeSuccess();
        product.StockQuantity.Should().Be(6);
    }

    [Fact]
    public void ReserveStock_WithInsufficientStock_Fails_AndDoesNotMutate()
    {
        var product = TestData.Product();
        product.AddStock(3).Discard();
        product.ReserveStock(5).Should().BeFailureOfType<Error.InvalidInput>();
        product.StockQuantity.Should().Be(3);
    }

    [Fact]
    public void ReleaseStock_RestoresStock()
    {
        var product = TestData.Product();
        product.AddStock(10).Discard();
        product.ReserveStock(4).Discard();
        product.ReleaseStock(4);
        product.StockQuantity.Should().Be(10);
    }
}
