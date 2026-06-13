namespace OrderManagement.Application.Tests;

using OrderManagement.Application.Customers;
using OrderManagement.Application.Orders;
using OrderManagement.Application.Products;
using OrderManagement.Domain;
using Trellis.Primitives;

public class HandlerBehaviorTests
{
    [Fact]
    public async Task CreateCustomer_WithDuplicateEmail_ReturnsConflict()
    {
        var fixture = new HandlerFixture();
        fixture.Customers.Add(TestData.Customer("dupe@example.com"));
        fixture.ActAsAdmin();

        var result = await fixture.Send(new CreateCustomerCommand(
            FirstName.Create("Jane"), LastName.Create("Doe"), EmailAddress.Create("dupe@example.com"),
            Maybe<PhoneNumber>.None,
            ShippingAddress.TryCreate("1 Main", "Town", "CA", "90001", "US").Unwrap()));

        result.Should().BeFailureOfType<Error.Conflict>();
    }

    [Fact]
    public async Task AddStock_WhenProductMissing_ReturnsNotFound()
    {
        var fixture = new HandlerFixture();
        fixture.ActAsAdmin();

        var result = await fixture.Send(new AddStockCommand(ProductId.NewUniqueV7(), 5));

        result.Should().BeFailureOfType<Error.NotFound>();
    }

    [Fact]
    public async Task GetOrderById_WhenMissing_ReturnsNotFound()
    {
        var fixture = new HandlerFixture();
        fixture.ActAsAdmin();

        var result = await fixture.Send(new GetOrderByIdQuery(OrderId.NewUniqueV7()));

        result.Should().BeFailureOfType<Error.NotFound>();
    }

    [Fact]
    public async Task CreateDraftOrder_CapturesUnitPrice_FromProduct()
    {
        var fixture = new HandlerFixture();
        var customer = TestData.Customer();
        var product = TestData.Product(price: 12.5m, stock: 10);
        fixture.Customers.Add(customer);
        fixture.Products.Add(product);
        fixture.ActAsAdmin();

        var result = await fixture.Send(new CreateDraftOrderCommand(
            customer.Id, [new OrderLineRequest(product.Id, Quantity.Create(3))]));

        var order = result.Unwrap();
        order.OrderTotal.Should().Be(37.5m);
        order.Status.Should().Be(OrderStatus.Draft);
    }

    [Fact]
    public async Task SubmitOrder_ReservesStock()
    {
        var fixture = new HandlerFixture();
        var customer = TestData.Customer();
        var product = TestData.Product(stock: 10);
        var order = TestData.OrderFor(customer, product, quantity: 4);
        fixture.Customers.Add(customer);
        fixture.Products.Add(product);
        fixture.Orders.Add(order);
        fixture.ActAsAdmin();

        var result = await fixture.Send(new SubmitOrderCommand(order.Id));

        result.Should().BeSuccess();
        order.Status.Should().Be(OrderStatus.Submitted);
        product.StockQuantity.Should().Be(6);
    }

    [Fact]
    public async Task SubmitOrder_WithInsufficientStock_FailsValidation()
    {
        var fixture = new HandlerFixture();
        var customer = TestData.Customer();
        var product = TestData.Product(stock: 2);
        var order = TestData.OrderFor(customer, product, quantity: 5);
        fixture.Customers.Add(customer);
        fixture.Products.Add(product);
        fixture.Orders.Add(order);
        fixture.ActAsAdmin();

        var result = await fixture.Send(new SubmitOrderCommand(order.Id));

        result.Should().BeFailureOfType<Error.InvalidInput>();
        product.StockQuantity.Should().Be(2);
    }

    [Fact]
    public async Task CancelOrder_AfterSubmit_ReleasesStock()
    {
        var fixture = new HandlerFixture();
        var customer = TestData.Customer();
        var product = TestData.Product(stock: 10);
        var order = TestData.OrderFor(customer, product, createdBy: "sales-1", quantity: 4);
        fixture.Customers.Add(customer);
        fixture.Products.Add(product);
        fixture.Orders.Add(order);
        fixture.ActAsAdmin();

        (await fixture.Send(new SubmitOrderCommand(order.Id))).Should().BeSuccess();
        product.StockQuantity.Should().Be(6);

        var result = await fixture.Send(new CancelOrderCommand(order.Id));

        result.Should().BeSuccess();
        order.Status.Should().Be(OrderStatus.Cancelled);
        product.StockQuantity.Should().Be(10);
    }
}
