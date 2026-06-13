using Moq;
using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Commands;
using OrderManagement.Application.Models;
using OrderManagement.Application.Queries;
using OrderManagement.Domain.Aggregates;
using OrderManagement.Domain.Enums;
using OrderManagement.Domain.Exceptions;
using OrderManagement.Domain.ValueObjects;
using Xunit;

namespace OrderManagement.Tests.Application;

public class AuthorizationTests
{
    private static ShippingAddress ValidAddress() =>
        new("123 Main St", "Springfield", "IL", "62701", "USA");

    private static Actor ActorWith(params string[] permissions) => new()
    {
        Id = "test-actor",
        Permissions = new HashSet<string>(permissions)
    };

    private static Actor AdminActor => ActorWith(
        Permissions.CustomersCreate, Permissions.ProductsCreate,
        Permissions.ProductsManageStock, Permissions.OrdersCreate,
        Permissions.OrdersSubmit, Permissions.OrdersApprove,
        Permissions.OrdersShip, Permissions.OrdersDeliver,
        Permissions.OrdersCancel, Permissions.OrdersRead, Permissions.OrdersReadAll);

    // --- Create Customer ---

    [Fact]
    public async Task CreateCustomer_WithPermission_Succeeds()
    {
        var repo = new Mock<ICustomerRepository>();
        repo.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), default)).ReturnsAsync((Customer?)null);
        repo.Setup(r => r.AddAsync(It.IsAny<Customer>(), default)).Returns(Task.CompletedTask);
        repo.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var handler = new CreateCustomerHandler(repo.Object);
        var cmd = new CreateCustomerCommand(
            ActorWith(Permissions.CustomersCreate),
            "John", "Doe", "john@test.com", null,
            "123 Main", "City", "State", "12345", "USA");

        var result = await handler.Handle(cmd, default);
        Assert.Equal("John", result.FirstName);
    }

    [Fact]
    public async Task CreateCustomer_WithoutPermission_ThrowsForbidden()
    {
        var repo = new Mock<ICustomerRepository>();
        var handler = new CreateCustomerHandler(repo.Object);
        var cmd = new CreateCustomerCommand(
            ActorWith("orders:read"),
            "John", "Doe", "john@test.com", null,
            "123 Main", "City", "State", "12345", "USA");

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(cmd, default));
    }

    // --- Create Product ---

    [Fact]
    public async Task CreateProduct_WithoutPermission_ThrowsForbidden()
    {
        var repo = new Mock<IProductRepository>();
        var handler = new CreateProductHandler(repo.Object);
        var cmd = new CreateProductCommand(ActorWith("orders:read"), "Widget", "WDG001", 9.99m);

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(cmd, default));
    }

    // --- Cancel Order - ownership ---

    [Fact]
    public async Task CancelOrder_ByOwner_Succeeds()
    {
        var now = DateTimeOffset.UtcNow;
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var actor = ActorWith(Permissions.OrdersCancel);

        var order = Order.Create(Guid.NewGuid(), actor.Id, now.AddMinutes(-1));
        order.AddLineItem(productId, "Widget", 2, 10m);

        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync(order);
        orderRepo.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var productRepo = new Mock<IProductRepository>();
        productRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync(new List<Product>());
        productRepo.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var handler = new CancelOrderHandler(orderRepo.Object, productRepo.Object);
        var result = await handler.Handle(
            new CancelOrderCommand(actor, orderId, now), default);

        Assert.Equal(OrderStatus.Cancelled, result.Status);
    }

    [Fact]
    public async Task CancelOrder_ByNonOwner_ThrowsForbidden()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = ActorWith(Permissions.OrdersCancel);

        var order = Order.Create(Guid.NewGuid(), "different-actor", now.AddMinutes(-1));
        order.AddLineItem(Guid.NewGuid(), "Widget", 1, 10m);

        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync(order);

        var productRepo = new Mock<IProductRepository>();

        var handler = new CancelOrderHandler(orderRepo.Object, productRepo.Object);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new CancelOrderCommand(actor, order.Id, now), default));
    }

    [Fact]
    public async Task CancelOrder_ByAdmin_Succeeds()
    {
        var now = DateTimeOffset.UtcNow;
        // Admin has both orders:cancel and orders:read-all
        var admin = ActorWith(Permissions.OrdersCancel, Permissions.OrdersReadAll);

        var order = Order.Create(Guid.NewGuid(), "someone-else", now.AddMinutes(-1));
        order.AddLineItem(Guid.NewGuid(), "Widget", 1, 10m);

        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync(order);
        orderRepo.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var productRepo = new Mock<IProductRepository>();
        productRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync(new List<Product>());
        productRepo.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var handler = new CancelOrderHandler(orderRepo.Object, productRepo.Object);
        var result = await handler.Handle(
            new CancelOrderCommand(admin, order.Id, now), default);

        Assert.Equal(OrderStatus.Cancelled, result.Status);
    }

    // --- Not Found ---

    [Fact]
    public async Task GetOrder_WhenNotFound_ThrowsNotFoundException()
    {
        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Order?)null);

        var handler = new GetOrderByIdHandler(orderRepo.Object);
        var query = new GetOrderByIdQuery(
            ActorWith(Permissions.OrdersRead), Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(query, default));
    }

    [Fact]
    public async Task AddStock_WhenProductNotFound_ThrowsNotFoundException()
    {
        var productRepo = new Mock<IProductRepository>();
        productRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Product?)null);

        var handler = new AddStockHandler(productRepo.Object);
        var cmd = new AddStockCommand(
            ActorWith(Permissions.ProductsManageStock), Guid.NewGuid(), 10);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(cmd, default));
    }
}
