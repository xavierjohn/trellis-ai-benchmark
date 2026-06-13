using NSubstitute;
using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Contracts;
using OrderManagement.Application.Orders;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Products;

namespace OrderManagement.Tests.Application;

public class OrderServiceTests
{
    private readonly IOrderRepository _orders = Substitute.For<IOrderRepository>();
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly TestTimeProvider _clock = new();

    private OrderService CreateService() => new(_orders, _customers, _products, _uow, _clock);

    private static Actor With(string id, params string[] permissions) => new(id, permissions);

    private Order BuildOrder(string creatorId, int stock = 10, int qty = 2)
    {
        var product = Product.Create("Widget", "WIDGET01", 9.99m, stock).Value;
        var requests = new List<(Order.LineRequest, Product)> { (new Order.LineRequest(product.Id, qty), product) };
        var order = Order.CreateDraft(Guid.NewGuid(), creatorId, requests, _clock.GetUtcNow()).Value;
        _products.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>()).Returns([product]);
        return order;
    }

    [Fact]
    public async Task Submit_WithPermission_Succeeds()
    {
        var order = BuildOrder("owner-1");
        _orders.GetByIdAsync(order.Id).Returns(order);
        var service = CreateService();

        var result = await service.SubmitAsync(With("wh-1", Permissions.OrdersSubmit), order.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(nameof(OrderStatus.Submitted), result.Value.Status);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Submit_WithoutPermission_Forbidden()
    {
        var order = BuildOrder("owner-1");
        _orders.GetByIdAsync(order.Id).Returns(order);
        var service = CreateService();

        var result = await service.SubmitAsync(With("wh-1"), order.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.Error!.Type);
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Submit_OrderNotFound_ReturnsNotFound()
    {
        _orders.GetByIdAsync(Arg.Any<Guid>()).Returns((Order?)null);
        var service = CreateService();

        var result = await service.SubmitAsync(With("wh-1", Permissions.OrdersSubmit), Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task Cancel_ByOwner_Succeeds()
    {
        var order = BuildOrder("owner-1");
        _orders.GetByIdAsync(order.Id).Returns(order);
        var service = CreateService();

        var result = await service.CancelAsync(With("owner-1", Permissions.OrdersCancel), order.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(nameof(OrderStatus.Cancelled), result.Value.Status);
    }

    [Fact]
    public async Task Cancel_ByNonOwnerWithoutReadAll_Forbidden()
    {
        var order = BuildOrder("owner-1");
        _orders.GetByIdAsync(order.Id).Returns(order);
        var service = CreateService();

        var result = await service.CancelAsync(With("intruder", Permissions.OrdersCancel), order.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.Error!.Type);
    }

    [Fact]
    public async Task Cancel_ByAdminNonOwner_Succeeds()
    {
        var order = BuildOrder("owner-1");
        _orders.GetByIdAsync(order.Id).Returns(order);
        var service = CreateService();

        var result = await service.CancelAsync(
            With("admin-1", Permissions.OrdersCancel, Permissions.OrdersReadAll), order.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(nameof(OrderStatus.Cancelled), result.Value.Status);
    }

    [Fact]
    public async Task CreateDraft_CustomerNotFound_ReturnsNotFound()
    {
        _customers.ExistsAsync(Arg.Any<Guid>()).Returns(false);
        var service = CreateService();

        var result = await service.CreateDraftAsync(
            With("sales-1", Permissions.OrdersCreate),
            new CreateOrderRequest(Guid.NewGuid(), [new OrderLineRequest(Guid.NewGuid(), 1)]));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task ListOverdue_ReturnsOnlyOverdueOrders()
    {
        var product = Product.Create("Widget", "WIDGET01", 9.99m, 100).Value;
        var requests = new List<(Order.LineRequest, Product)> { (new Order.LineRequest(product.Id, 1), product) };
        var overdue = Order.CreateDraft(Guid.NewGuid(), "owner-1", requests, _clock.GetUtcNow()).Value;
        overdue.Submit(new Dictionary<Guid, Product> { [product.Id] = product }, _clock.GetUtcNow());

        var service = CreateService();
        _orders.GetSubmittedBeforeAsync(Arg.Any<DateTimeOffset>()).Returns([overdue]);

        // Advance clock far beyond submission to make it overdue.
        _clock.Advance(TimeSpan.FromDays(30));

        var result = await service.ListOverdueAsync(With("wh-1", Permissions.OrdersReadAll));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
    }
}
