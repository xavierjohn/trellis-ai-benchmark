using Application.Common;
using Application.Customers;
using Application.Interfaces;
using Application.Orders;
using Domain.Orders;
using Domain.Products;
using NSubstitute;

namespace Application.Tests;

public class AuthorizationTests
{
    [Fact]
    public async Task CreateCustomer_WithPermission_Succeeds()
    {
        var repo = Substitute.For<ICustomerRepository>();
        repo.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreateCustomerHandler(repo);
        var actor = new Actor("actor-1", ["customers:create"]);
        var cmd = new CreateCustomerCommand("John", "Doe", "john@example.com", null, "123 St", "City", "State", "12345", "US");

        var result = await handler.HandleAsync(cmd, actor);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CreateCustomer_WithoutPermission_Fails()
    {
        var repo = Substitute.For<ICustomerRepository>();
        var handler = new CreateCustomerHandler(repo);
        var actor = new Actor("actor-1", ["orders:read"]);
        var cmd = new CreateCustomerCommand("John", "Doe", "john@example.com", null, "123 St", "City", "State", "12345", "US");

        var result = await handler.HandleAsync(cmd, actor);
        Assert.False(result.IsSuccess);
        Assert.Equal("forbidden", result.ErrorCode);
    }

    [Fact]
    public async Task CancelOrder_ByOwner_Succeeds()
    {
        var orderRepo = Substitute.For<IOrderRepository>();
        var productRepo = Substitute.For<IProductRepository>();
        var order = CreateDraftOrder("actor-1");
        orderRepo.GetByIdAsync(order.OrderId, Arg.Any<CancellationToken>()).Returns(order);
        productRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns([]);

        var handler = new CancelOrderHandler(orderRepo, productRepo);
        var actor = new Actor("actor-1", ["orders:cancel"]);

        var result = await handler.HandleAsync(order.OrderId, actor);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CancelOrder_ByNonOwner_Fails()
    {
        var orderRepo = Substitute.For<IOrderRepository>();
        var productRepo = Substitute.For<IProductRepository>();
        var order = CreateDraftOrder("actor-1");
        orderRepo.GetByIdAsync(order.OrderId, Arg.Any<CancellationToken>()).Returns(order);

        var handler = new CancelOrderHandler(orderRepo, productRepo);
        var actor = new Actor("actor-2", ["orders:cancel"]);

        var result = await handler.HandleAsync(order.OrderId, actor);
        Assert.False(result.IsSuccess);
        Assert.Equal("forbidden", result.ErrorCode);
    }

    [Fact]
    public async Task CancelOrder_ByAdminWithReadAll_Succeeds()
    {
        var orderRepo = Substitute.For<IOrderRepository>();
        var productRepo = Substitute.For<IProductRepository>();
        var order = CreateDraftOrder("actor-1");
        orderRepo.GetByIdAsync(order.OrderId, Arg.Any<CancellationToken>()).Returns(order);
        productRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns([]);

        var handler = new CancelOrderHandler(orderRepo, productRepo);
        var actor = new Actor("admin", ["orders:cancel", "orders:read-all"]);

        var result = await handler.HandleAsync(order.OrderId, actor);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task GetOrder_WhenNotFound_ReturnsNotFound()
    {
        var orderRepo = Substitute.For<IOrderRepository>();
        orderRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Order?)null);

        var handler = new GetOrderHandler(orderRepo);
        var actor = new Actor("actor-1", ["orders:read"]);

        var result = await handler.HandleAsync(Guid.NewGuid(), actor);
        Assert.False(result.IsSuccess);
        Assert.Equal("not_found", result.ErrorCode);
    }

    private static Order CreateDraftOrder(string actorId)
    {
        var order = Order.Create(Guid.NewGuid(), actorId, TimeProvider.System).Value!;
        order.AddLineItem(Guid.NewGuid(), "Widget", 1, 9.99m);
        return order;
    }
}
