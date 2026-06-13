using NSubstitute;
using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Authorization;
using OrderManagement.Application.Contracts;
using OrderManagement.Application.Products;
using OrderManagement.Domain.Common;

namespace OrderManagement.Tests.Application;

public class ProductServiceAuthorizationTests
{
    private static CreateProductRequest ValidRequest() => new("Widget", "WIDGET01", 9.99m);

    private static ProductService Build(IActorProvider actor, IProductRepository? products = null)
    {
        products ??= Substitute.For<IProductRepository>();
        products.ExistsBySkuAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        var uow = Substitute.For<IUnitOfWork>();
        return new ProductService(products, uow, actor);
    }

    [Fact]
    public async Task Create_WithRequiredPermission_Succeeds()
    {
        var service = Build(new StaticActorProvider("u1", Permissions.ProductsCreate));

        var result = await service.CreateAsync(ValidRequest());

        Assert.True(result.IsSuccess);
        Assert.Equal("WIDGET01", result.Value.Sku);
    }

    [Fact]
    public async Task Create_WithoutPermission_ReturnsForbidden()
    {
        var service = Build(new StaticActorProvider("u1", Permissions.OrdersRead));

        var result = await service.CreateAsync(ValidRequest());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Forbidden, result.Error!.Kind);
    }

    [Fact]
    public async Task AddStock_WhenProductNotFound_ReturnsNotFound()
    {
        var products = Substitute.For<IProductRepository>();
        products.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((OrderManagement.Domain.Products.Product?)null);
        var service = Build(new StaticActorProvider("u1", Permissions.ProductsManageStock), products);

        var result = await service.AddStockAsync(Guid.NewGuid(), new AddStockRequest(5));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.NotFound, result.Error!.Kind);
    }
}
