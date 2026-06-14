using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using OrderManagement.Api.Application.Abstractions;
using OrderManagement.Api.Application.Auth;
using OrderManagement.Api.Application.Orders;
using OrderManagement.Api.Contracts;
using OrderManagement.Api.Domain.Common;
using OrderManagement.Api.Domain.Customers;
using OrderManagement.Api.Domain.Orders;
using OrderManagement.Api.Domain.Products;
using Xunit;

namespace OrderManagement.Tests.Application;

public class OrderServiceTests
{
    private readonly IOrderRepository _orders = Substitute.For<IOrderRepository>();
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

    private OrderService CreateService() => new(_orders, _products, _customers, _uow, _time);

    private static Actor ActorWith(string id, params string[] permissions) => new(id, permissions);

    private Order BuildDraftOrder(string createdBy)
    {
        var product = Product.Create("Widget", "WIDGET01", 10m, 100);
        var drafts = new List<LineItemDraft> { new(product.Id, "Widget", 10m, 2) };
        return Order.CreateDraft(Guid.NewGuid(), createdBy, drafts, _time);
    }

    // ----- Authorization -----

    [Fact]
    public async Task CreateDraft_WithPermission_Succeeds()
    {
        var customer = new TestCustomer().Customer;
        var product = Product.Create("Widget", "WIDGET01", 10m, 100);
        _customers.GetByIdAsync(customer.Id).Returns(customer);
        _products.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>())
            .Returns(new List<Product> { product });

        var service = CreateService();
        var request = new CreateOrderRequest(customer.Id, new List<CreateOrderLineItemDto> { new(product.Id, 2) });

        var order = await service.CreateDraftAsync(ActorWith("sales-1", Permissions.OrdersCreate), request);

        Assert.Equal(OrderStatus.Draft, order.Status);
        Assert.Equal("sales-1", order.CreatedByActorId);
        await _orders.Received(1).AddAsync(Arg.Any<Order>());
        await _uow.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task CreateDraft_WithoutPermission_ThrowsForbidden()
    {
        var service = CreateService();
        var request = new CreateOrderRequest(Guid.NewGuid(), new List<CreateOrderLineItemDto> { new(Guid.NewGuid(), 1) });

        await Assert.ThrowsAsync<ForbiddenAppException>(() =>
            service.CreateDraftAsync(ActorWith("nobody", Permissions.OrdersRead), request));
    }

    // ----- Resource authorization (cancel ownership) -----

    [Fact]
    public async Task Cancel_ByOwner_Succeeds()
    {
        var order = BuildDraftOrder("owner-1");
        _orders.GetByIdAsync(order.Id).Returns(order);

        var service = CreateService();
        var result = await service.CancelAsync(ActorWith("owner-1", Permissions.OrdersCancel), order.Id);

        Assert.Equal(OrderStatus.Cancelled, result.Status);
    }

    [Fact]
    public async Task Cancel_ByNonOwner_ThrowsForbidden()
    {
        var order = BuildDraftOrder("owner-1");
        _orders.GetByIdAsync(order.Id).Returns(order);

        var service = CreateService();

        await Assert.ThrowsAsync<ForbiddenAppException>(() =>
            service.CancelAsync(ActorWith("intruder", Permissions.OrdersCancel), order.Id));
        Assert.Equal(OrderStatus.Draft, order.Status);
    }

    [Fact]
    public async Task Cancel_ByAdmin_NonOwner_Succeeds()
    {
        var order = BuildDraftOrder("owner-1");
        _orders.GetByIdAsync(order.Id).Returns(order);

        var service = CreateService();
        var admin = ActorWith("admin", Permissions.OrdersCancel, Permissions.OrdersReadAll);

        var result = await service.CancelAsync(admin, order.Id);

        Assert.Equal(OrderStatus.Cancelled, result.Status);
    }

    [Fact]
    public async Task Cancel_FromSubmitted_ReleasesStock()
    {
        var product = Product.Create("Widget", "WIDGET01", 10m, 100);
        var drafts = new List<LineItemDraft> { new(product.Id, "Widget", 10m, 5) };
        var order = Order.CreateDraft(Guid.NewGuid(), "owner-1", drafts, _time);
        order.Submit(new Dictionary<Guid, Product> { [product.Id] = product }, _time);
        Assert.Equal(95, product.StockQuantity);

        _orders.GetByIdAsync(order.Id).Returns(order);
        _products.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>())
            .Returns(new List<Product> { product });

        var service = CreateService();
        await service.CancelAsync(ActorWith("owner-1", Permissions.OrdersCancel), order.Id);

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(100, product.StockQuantity);
    }

    // ----- Not found -----

    [Fact]
    public async Task GetById_WhenMissing_ThrowsNotFound()
    {
        _orders.GetByIdAsync(Arg.Any<Guid>()).Returns((Order?)null);
        var service = CreateService();

        await Assert.ThrowsAsync<NotFoundAppException>(() =>
            service.GetByIdAsync(ActorWith("reader", Permissions.OrdersRead), Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateDraft_WhenCustomerMissing_ThrowsNotFound()
    {
        _customers.GetByIdAsync(Arg.Any<Guid>()).Returns((Customer?)null);
        var service = CreateService();
        var request = new CreateOrderRequest(Guid.NewGuid(), new List<CreateOrderLineItemDto> { new(Guid.NewGuid(), 1) });

        await Assert.ThrowsAsync<NotFoundAppException>(() =>
            service.CreateDraftAsync(ActorWith("sales-1", Permissions.OrdersCreate), request));
    }

    [Fact]
    public async Task Submit_WhenOrderMissing_ThrowsNotFound()
    {
        _orders.GetByIdAsync(Arg.Any<Guid>()).Returns((Order?)null);
        var service = CreateService();

        await Assert.ThrowsAsync<NotFoundAppException>(() =>
            service.SubmitAsync(ActorWith("sales-1", Permissions.OrdersSubmit), Guid.NewGuid()));
    }

    private sealed class TestCustomer
    {
        public Customer Customer { get; } = Customer.Create(
            "Jane", "Doe", "jane@example.com", null,
            new ShippingAddress("1 St", "City", "ST", "00000", "USA"));
    }
}
