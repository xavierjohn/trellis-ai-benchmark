namespace Application.Tests;

using Mediator;
using OrderManagement.Application.Customers;
using OrderManagement.Application.Orders;
using OrderManagement.Application.Products;
using OrderManagement.Domain;
using Trellis.Authorization;
using Trellis.Primitives;
using Trellis.Testing;

public class OrderLifecycleTests
{
    private readonly ISender _sender;
    private readonly TestActorProvider _actorProvider;
    private readonly FakeRepository<Order, OrderId> _orderRepo;
    private readonly FakeRepository<Product, ProductId> _productRepo;

    public OrderLifecycleTests(
        ISender sender,
        TestActorProvider actorProvider,
        FakeRepository<Order, OrderId> orderRepo,
        FakeRepository<Product, ProductId> productRepo)
    {
        _sender = sender;
        _actorProvider = actorProvider;
        _orderRepo = orderRepo;
        _productRepo = productRepo;
    }

    private static ShippingAddress DefaultAddress() =>
        ShippingAddress.TryCreate("123 Main St", "Springfield", "IL", "62701", "US").GetValueOrThrow();

    private async Task<Customer> CreateCustomerAsync(string email = "customer@example.com")
    {
        var command = new CreateCustomerCommand(
            FirstName.Create("Test"),
            LastName.Create("Customer"),
            EmailAddress.Create(email),
            Maybe<PhoneNumber>.None,
            DefaultAddress());
        return (await _sender.Send(command, TestContext.Current.CancellationToken)).Unwrap();
    }

    private async Task<Product> CreateProductAsync(string sku = "TESTSKU01")
    {
        var createCmd = new CreateProductCommand(
            ProductName.Create("Test Product"),
            SKU.Create(sku),
            UnitPrice.Create(10.00m));
        var product = (await _sender.Send(createCmd, TestContext.Current.CancellationToken)).Unwrap();

        var stockCmd = new AddStockCommand(product.Id, Quantity.Create(100));
        (await _sender.Send(stockCmd, TestContext.Current.CancellationToken)).Unwrap();

        return product;
    }

    [Fact]
    public async Task CreateDraftOrder_with_valid_inputs_succeeds()
    {
        var customer = await CreateCustomerAsync("order-draft@example.com");
        var product = await CreateProductAsync("ORDTEST01");

        var command = new CreateDraftOrderCommand(
            customer.Id,
            [(product.Id, Quantity.Create(2))]);

        var result = await _sender.Send(command, TestContext.Current.CancellationToken);

        result.Should().BeSuccess();
        var order = result.Unwrap();
        order.CustomerId.Should().Be(customer.Id);
        order.Status.Should().Be(OrderStatus.Draft);
        order.LineItems.Should().HaveCount(1);
        order.OrderTotal.Should().Be(20.00m);
    }

    [Fact]
    public async Task FullLifecycle_draft_to_delivered_succeeds()
    {
        var customer = await CreateCustomerAsync("lifecycle@example.com");
        var product = await CreateProductAsync("LIFECYCLE1");

        var order = (await _sender.Send(
            new CreateDraftOrderCommand(customer.Id, [(product.Id, Quantity.Create(1))]),
            TestContext.Current.CancellationToken)).Unwrap();

        // Submit
        var submitResult = await _sender.Send(new SubmitOrderCommand(order.Id), TestContext.Current.CancellationToken);
        submitResult.Should().BeSuccess();
        submitResult.Unwrap().Status.Should().Be(OrderStatus.Submitted);

        // Approve
        var approveResult = await _sender.Send(new ApproveOrderCommand(order.Id), TestContext.Current.CancellationToken);
        approveResult.Should().BeSuccess();
        approveResult.Unwrap().Status.Should().Be(OrderStatus.Approved);

        // Ship
        var shipResult = await _sender.Send(new ShipOrderCommand(order.Id), TestContext.Current.CancellationToken);
        shipResult.Should().BeSuccess();
        shipResult.Unwrap().Status.Should().Be(OrderStatus.Shipped);

        // Deliver
        var deliverResult = await _sender.Send(new DeliverOrderCommand(order.Id), TestContext.Current.CancellationToken);
        deliverResult.Should().BeSuccess();
        deliverResult.Unwrap().Status.Should().Be(OrderStatus.Delivered);
    }

    [Fact]
    public async Task SubmitOrder_reserves_stock()
    {
        var customer = await CreateCustomerAsync("reservestock@example.com");
        var product = await CreateProductAsync("RESERVE001");

        var order = (await _sender.Send(
            new CreateDraftOrderCommand(customer.Id, [(product.Id, Quantity.Create(5))]),
            TestContext.Current.CancellationToken)).Unwrap();

        _ = (await _sender.Send(new SubmitOrderCommand(order.Id), TestContext.Current.CancellationToken)).Unwrap();

        var storedProduct = _productRepo.GetAll().First(p => p.Id == product.Id);
        storedProduct.StockQuantity.Value.Should().Be(95); // 100 - 5
    }

    [Fact]
    public async Task SubmitOrder_with_insufficient_stock_fails()
    {
        var customer = await CreateCustomerAsync("insuffstock@example.com");
        var createCmd = new CreateProductCommand(
            ProductName.Create("Low Stock Product"),
            SKU.Create("LOWSTOCK1"),
            UnitPrice.Create(5.00m));
        var product = (await _sender.Send(createCmd, TestContext.Current.CancellationToken)).Unwrap();
        // No stock added - quantity is 0

        var order = (await _sender.Send(
            new CreateDraftOrderCommand(customer.Id, [(product.Id, Quantity.Create(1))]),
            TestContext.Current.CancellationToken)).Unwrap();

        var submitResult = await _sender.Send(new SubmitOrderCommand(order.Id), TestContext.Current.CancellationToken);

        submitResult.Should().BeFailure();
    }

    [Fact]
    public async Task Cancel_submitted_order_releases_stock()
    {
        var customer = await CreateCustomerAsync("cancelstock@example.com");
        var product = await CreateProductAsync("CANCELSTK1");

        var order = (await _sender.Send(
            new CreateDraftOrderCommand(customer.Id, [(product.Id, Quantity.Create(3))]),
            TestContext.Current.CancellationToken)).Unwrap();

        _ = (await _sender.Send(new SubmitOrderCommand(order.Id), TestContext.Current.CancellationToken)).Unwrap();

        var cancelResult = await _sender.Send(new CancelOrderCommand(order.Id), TestContext.Current.CancellationToken);

        cancelResult.Should().BeSuccess();
        cancelResult.Unwrap().Status.Should().Be(OrderStatus.Cancelled);

        var storedProduct = _productRepo.GetAll().First(p => p.Id == product.Id);
        storedProduct.StockQuantity.Value.Should().Be(100); // restored
    }

    [Fact]
    public async Task AddLineItem_to_draft_order_succeeds()
    {
        var customer = await CreateCustomerAsync("addline@example.com");
        var product1 = await CreateProductAsync("ADDLINE001");
        var product2 = await CreateProductAsync("ADDLINE002");

        var order = (await _sender.Send(
            new CreateDraftOrderCommand(customer.Id, [(product1.Id, Quantity.Create(1))]),
            TestContext.Current.CancellationToken)).Unwrap();

        var addResult = await _sender.Send(
            new AddLineItemCommand(order.Id, product2.Id, Quantity.Create(2)),
            TestContext.Current.CancellationToken);

        addResult.Should().BeSuccess();
        addResult.Unwrap().LineItems.Should().HaveCount(2);
    }

    [Fact]
    public async Task RemoveLineItem_from_draft_order_succeeds()
    {
        var customer = await CreateCustomerAsync("removeline@example.com");
        var product1 = await CreateProductAsync("REMLINE001");
        var product2 = await CreateProductAsync("REMLINE002");

        var order = (await _sender.Send(
            new CreateDraftOrderCommand(customer.Id, [(product1.Id, Quantity.Create(1)), (product2.Id, Quantity.Create(1))]),
            TestContext.Current.CancellationToken)).Unwrap();

        var lineItemToRemove = order.LineItems.First(li => li.ProductId == product1.Id);
        var removeResult = await _sender.Send(
            new RemoveLineItemCommand(order.Id, lineItemToRemove.Id),
            TestContext.Current.CancellationToken);

        removeResult.Should().BeSuccess();
        removeResult.Unwrap().LineItems.Should().HaveCount(1);
    }
}
