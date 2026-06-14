namespace Application.Tests;

using Mediator;
using OrderManagement.Application.Orders;
using OrderManagement.Application.Products;
using OrderManagement.Domain;
using Trellis.Testing;

public class OrderHandlerTests
{
    private readonly ISender _sender;
    private readonly TestActorProvider _actor;
    private readonly FakeRepository<Product, ProductId> _products;
    private readonly FakeRepository<Order, OrderId> _orders;

    public OrderHandlerTests(
        ISender sender,
        TestActorProvider actor,
        FakeRepository<Product, ProductId> products,
        FakeRepository<Order, OrderId> orders)
    {
        _sender = sender;
        _actor = actor;
        _products = products;
        _orders = orders;
    }

    private static Product NewProduct(string sku = "ABC123", int stock = 100) =>
        Product.Create(ProductName.Create("Widget"), Sku.Create(sku), UnitPrice.Create(9.99m), StockQuantity.Create(stock));

    private Order SeedDraftOrder(string createdBy)
    {
        var product = NewProduct();
        _products.Add(product);
        var line = new DraftLineItem(product.Id, product.Name, Quantity.Create(1), product.UnitPrice);
        var order = Order.CreateDraft(CustomerId.NewUniqueV7(), createdBy, [line]).Unwrap();
        _orders.Add(order);
        return order;
    }

    [Fact]
    public async Task Create_product_with_permission_succeeds()
    {
        await using var _ = _actor.WithActor("wh", Permissions.ProductsCreate);

        var result = await _sender.Send(
            new CreateProductCommand(ProductName.Create("Gadget"), Sku.Create("SKU001"), UnitPrice.Create(5m)),
            TestContext.Current.CancellationToken);

        result.Should().BeSuccess();
    }

    [Fact]
    public async Task Create_product_without_permission_returns_forbidden()
    {
        await using var _ = _actor.WithActor("nobody", Permissions.OrdersRead);

        var result = await _sender.Send(
            new CreateProductCommand(ProductName.Create("Gadget"), Sku.Create("SKU002"), UnitPrice.Create(5m)),
            TestContext.Current.CancellationToken);

        result.Should().BeFailureOfType<Error.Forbidden>();
    }

    [Fact]
    public async Task Cancel_by_owner_succeeds()
    {
        var order = SeedDraftOrder("owner-1");
        await using var _ = _actor.WithActor("owner-1", Permissions.OrdersCancel);

        var result = await _sender.Send(new CancelOrderCommand(order.Id), TestContext.Current.CancellationToken);

        result.Should().BeSuccess();
        result.Unwrap().Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public async Task Cancel_by_non_owner_returns_forbidden()
    {
        var order = SeedDraftOrder("owner-1");
        await using var _ = _actor.WithActor("intruder", Permissions.OrdersCancel);

        var result = await _sender.Send(new CancelOrderCommand(order.Id), TestContext.Current.CancellationToken);

        result.Should().BeFailureOfType<Error.Forbidden>();
    }

    [Fact]
    public async Task Cancel_by_admin_succeeds()
    {
        var order = SeedDraftOrder("owner-1");
        await using var _ = _actor.WithActor("admin-2", Permissions.OrdersCancel, Permissions.OrdersReadAll);

        var result = await _sender.Send(new CancelOrderCommand(order.Id), TestContext.Current.CancellationToken);

        result.Should().BeSuccess();
    }

    [Fact]
    public async Task Get_order_not_found_returns_not_found()
    {
        var result = await _sender.Send(
            new GetOrderByIdQuery(OrderId.NewUniqueV7()), TestContext.Current.CancellationToken);

        result.Should().BeFailureOfType<Error.NotFound>();
    }
}
