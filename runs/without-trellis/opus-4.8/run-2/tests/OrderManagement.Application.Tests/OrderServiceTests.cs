using Moq;
using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Orders;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Products;
using Xunit;

namespace OrderManagement.Application.Tests;

public class OrderServiceTests
{
    private static OrderService BuildService(
        out Mock<IOrderRepository> orders,
        out Mock<ICustomerRepository> customers,
        out Mock<IProductRepository> products,
        out Mock<IUnitOfWork> uow)
    {
        orders = new Mock<IOrderRepository>();
        customers = new Mock<ICustomerRepository>();
        products = new Mock<IProductRepository>();
        uow = new Mock<IUnitOfWork>();
        return new OrderService(orders.Object, customers.Object, products.Object, uow.Object, Mocks.Clock());
    }

    [Fact]
    public async Task CreateDraft_with_permission_succeeds()
    {
        var service = BuildService(out var orders, out var customers, out var products, out _);
        var customer = Mocks.Customer();
        var product = Mocks.Product();
        customers.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);
        products.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { product });

        var request = new CreateOrderRequest(customer.Id, new[] { new OrderLineRequest(product.Id, 2) });
        var result = await service.CreateDraftAsync(Mocks.Actor("sales-1", Permissions.OrdersCreate), request);

        Assert.True(result.IsSuccess);
        Assert.Equal("sales-1", result.Value.CreatedByActorId);
        orders.Verify(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateDraft_without_permission_is_forbidden()
    {
        var service = BuildService(out _, out _, out _, out _);
        var request = new CreateOrderRequest(Guid.NewGuid(), new[] { new OrderLineRequest(Guid.NewGuid(), 1) });

        var result = await service.CreateDraftAsync(Mocks.Actor("u1", Permissions.OrdersRead), request);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Forbidden, result.Error!.Kind);
    }

    [Fact]
    public async Task CreateDraft_with_unknown_customer_is_not_found()
    {
        var service = BuildService(out _, out var customers, out _, out _);
        customers.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var request = new CreateOrderRequest(Guid.NewGuid(), new[] { new OrderLineRequest(Guid.NewGuid(), 1) });
        var result = await service.CreateDraftAsync(Mocks.Admin(), request);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.NotFound, result.Error!.Kind);
    }

    [Fact]
    public async Task Submit_unknown_order_is_not_found()
    {
        var service = BuildService(out var orders, out _, out _, out _);
        orders.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);

        var result = await service.SubmitAsync(Mocks.Admin(), Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.NotFound, result.Error!.Kind);
    }

    [Fact]
    public async Task Cancel_by_owner_succeeds()
    {
        var service = BuildService(out var orders, out _, out var products, out _);
        var customer = Mocks.Customer();
        var product = Mocks.Product();
        var order = Mocks.DraftOrder(customer, product, "owner-1");
        orders.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        products.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { product });

        var result = await service.CancelAsync(Mocks.Actor("owner-1", Permissions.OrdersCancel), order.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal("Cancelled", result.Value.Status);
    }

    [Fact]
    public async Task Cancel_by_non_owner_is_forbidden()
    {
        var service = BuildService(out var orders, out _, out _, out _);
        var customer = Mocks.Customer();
        var product = Mocks.Product();
        var order = Mocks.DraftOrder(customer, product, "owner-1");
        orders.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        // Has orders:cancel but is not the owner and lacks orders:read-all.
        var result = await service.CancelAsync(Mocks.Actor("other-1", Permissions.OrdersCancel), order.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Forbidden, result.Error!.Kind);
        Assert.Equal(OrderStatus.Draft, order.Status);
    }

    [Fact]
    public async Task Cancel_by_admin_non_owner_succeeds()
    {
        var service = BuildService(out var orders, out _, out var products, out _);
        var customer = Mocks.Customer();
        var product = Mocks.Product();
        var order = Mocks.DraftOrder(customer, product, "owner-1");
        orders.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        products.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { product });

        // Admin: has both orders:cancel and orders:read-all.
        var admin = Mocks.Actor("admin-9", Permissions.OrdersCancel, Permissions.OrdersReadAll);
        var result = await service.CancelAsync(admin, order.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal("Cancelled", result.Value.Status);
    }

    [Fact]
    public async Task Cancel_without_permission_is_forbidden()
    {
        var service = BuildService(out _, out _, out _, out _);

        var result = await service.CancelAsync(Mocks.Actor("u1", Permissions.OrdersRead), Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Forbidden, result.Error!.Kind);
    }
}
