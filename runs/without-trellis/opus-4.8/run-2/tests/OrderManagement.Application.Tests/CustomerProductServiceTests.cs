using Moq;
using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Customers;
using OrderManagement.Application.Products;
using OrderManagement.Domain.Common;
using Xunit;

namespace OrderManagement.Application.Tests;

public class CustomerProductServiceTests
{
    private static CreateCustomerRequest CustomerRequest(string email = "jane@example.com") =>
        new("Jane", "Doe", email, null, new AddressRequest("1 Main", "Town", "CA", "90001", "US"));

    [Fact]
    public async Task CreateCustomer_with_permission_succeeds()
    {
        var repo = new Mock<ICustomerRepository>();
        repo.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var uow = new Mock<IUnitOfWork>();
        var service = new CustomerService(repo.Object, uow.Object);

        var result = await service.CreateAsync(Mocks.Actor("u1", Permissions.CustomersCreate), CustomerRequest());

        Assert.True(result.IsSuccess);
        repo.Verify(r => r.AddAsync(It.IsAny<Domain.Customers.Customer>(), It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateCustomer_without_permission_is_forbidden()
    {
        var repo = new Mock<ICustomerRepository>();
        var uow = new Mock<IUnitOfWork>();
        var service = new CustomerService(repo.Object, uow.Object);

        var result = await service.CreateAsync(Mocks.Actor("u1", Permissions.OrdersRead), CustomerRequest());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Forbidden, result.Error!.Kind);
        repo.Verify(r => r.AddAsync(It.IsAny<Domain.Customers.Customer>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateCustomer_with_duplicate_email_is_conflict()
    {
        var repo = new Mock<ICustomerRepository>();
        repo.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var uow = new Mock<IUnitOfWork>();
        var service = new CustomerService(repo.Object, uow.Object);

        var result = await service.CreateAsync(Mocks.Admin(), CustomerRequest());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Conflict, result.Error!.Kind);
    }

    [Fact]
    public async Task AddStock_to_unknown_product_is_not_found()
    {
        var repo = new Mock<IProductRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Products.Product?)null);
        var uow = new Mock<IUnitOfWork>();
        var service = new ProductService(repo.Object, uow.Object);

        var result = await service.AddStockAsync(Mocks.Admin(), Guid.NewGuid(), new AddStockRequest(5));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.NotFound, result.Error!.Kind);
    }

    [Fact]
    public async Task CreateProduct_with_duplicate_sku_is_conflict()
    {
        var repo = new Mock<IProductRepository>();
        repo.Setup(r => r.ExistsBySkuAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var uow = new Mock<IUnitOfWork>();
        var service = new ProductService(repo.Object, uow.Object);

        var result = await service.CreateAsync(Mocks.Admin(), new CreateProductRequest("Widget", "SKU123", 5m));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Conflict, result.Error!.Kind);
    }
}
