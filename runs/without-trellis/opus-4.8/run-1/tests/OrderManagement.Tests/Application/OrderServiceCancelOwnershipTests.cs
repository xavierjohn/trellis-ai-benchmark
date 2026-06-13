using NSubstitute;
using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Authorization;
using OrderManagement.Application.Orders;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Products;
using static OrderManagement.Tests.Domain.DomainFixtures;

namespace OrderManagement.Tests.Application;

public class OrderServiceCancelOwnershipTests
{
    private static OrderService Build(IActorProvider actor, Order order)
    {
        var orders = Substitute.For<IOrderRepository>();
        orders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        var products = Substitute.For<IProductRepository>();
        products.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Product>());
        var customers = Substitute.For<ICustomerRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        return new OrderService(orders, customers, products, uow, actor, TimeProvider.System);
    }

    private static Order DraftOrderBy(string actorId)
    {
        var p = ProductWithStock("SKU001", 10m, 50);
        return DraftOrder(Guid.NewGuid(), actorId, (p, 2));
    }

    [Fact]
    public async Task Cancel_ByOwner_Succeeds()
    {
        var order = DraftOrderBy("owner-1");
        var service = Build(new StaticActorProvider("owner-1", Permissions.OrdersCancel), order);

        var result = await service.CancelAsync(order.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(nameof(OrderStatus.Cancelled), result.Value.Status);
    }

    [Fact]
    public async Task Cancel_ByNonOwnerWithoutAdmin_ReturnsForbidden()
    {
        var order = DraftOrderBy("owner-1");
        var service = Build(new StaticActorProvider("intruder", Permissions.OrdersCancel), order);

        var result = await service.CancelAsync(order.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Forbidden, result.Error!.Kind);
    }

    [Fact]
    public async Task Cancel_ByAdminNonOwner_Succeeds()
    {
        var order = DraftOrderBy("owner-1");
        var service = Build(
            new StaticActorProvider("admin-1", Permissions.OrdersCancel, Permissions.OrdersReadAll), order);

        var result = await service.CancelAsync(order.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(nameof(OrderStatus.Cancelled), result.Value.Status);
    }

    [Fact]
    public async Task Cancel_WhenOrderNotFound_ReturnsNotFound()
    {
        var orders = Substitute.For<IOrderRepository>();
        orders.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Order?)null);
        var service = new OrderService(
            orders,
            Substitute.For<ICustomerRepository>(),
            Substitute.For<IProductRepository>(),
            Substitute.For<IUnitOfWork>(),
            new StaticActorProvider("admin-1", Permissions.OrdersCancel, Permissions.OrdersReadAll),
            TimeProvider.System);

        var result = await service.CancelAsync(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.NotFound, result.Error!.Kind);
    }
}
