using NSubstitute;
using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Contracts;
using OrderManagement.Application.Products;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Products;

namespace OrderManagement.Tests.Application;

public class ProductServiceTests
{
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private ProductService CreateService() => new(_products, _uow);

    private static Actor With(params string[] permissions) => new("actor-1", permissions);

    [Fact]
    public async Task Create_WithPermission_Succeeds()
    {
        _products.SkuExistsAsync("WIDGET01").Returns(false);
        var service = CreateService();

        var result = await service.CreateAsync(
            With(Permissions.ProductsCreate),
            new CreateProductRequest("Widget", "WIDGET01", 9.99m));

        Assert.True(result.IsSuccess);
        await _products.Received(1).AddAsync(Arg.Any<Product>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_WithoutPermission_Forbidden()
    {
        var service = CreateService();

        var result = await service.CreateAsync(
            With(), // no permissions
            new CreateProductRequest("Widget", "WIDGET01", 9.99m));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.Error!.Type);
        await _products.DidNotReceive().AddAsync(Arg.Any<Product>());
    }

    [Fact]
    public async Task Create_WithDuplicateSku_Conflict()
    {
        _products.SkuExistsAsync("WIDGET01").Returns(true);
        var service = CreateService();

        var result = await service.CreateAsync(
            With(Permissions.ProductsCreate),
            new CreateProductRequest("Widget", "WIDGET01", 9.99m));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error!.Type);
    }

    [Fact]
    public async Task AddStock_ProductNotFound_ReturnsNotFound()
    {
        _products.GetByIdAsync(Arg.Any<Guid>()).Returns((Product?)null);
        var service = CreateService();

        var result = await service.AddStockAsync(
            With(Permissions.ProductsManageStock), Guid.NewGuid(), new AddStockRequest(5));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }
}
