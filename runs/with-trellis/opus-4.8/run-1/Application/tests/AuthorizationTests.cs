namespace OrderManagement.Application.Tests;

using OrderManagement.Application.Orders;
using OrderManagement.Application.Products;
using OrderManagement.Domain;

public class AuthorizationTests
{
    [Fact]
    public async Task Command_WithRequiredPermission_Succeeds()
    {
        var fixture = new HandlerFixture();
        fixture.ActAs("wh-1", Permissions.ProductsCreate);

        var result = await fixture.Send(new CreateProductCommand(
            ProductName.Create("Widget"), Sku.Create("WIDGET01"), UnitPrice.Create(9.99m)));

        result.Should().BeSuccess();
    }

    [Fact]
    public async Task Command_WithMissingPermission_IsForbidden()
    {
        var fixture = new HandlerFixture();
        fixture.ActAs("nobody");

        var result = await fixture.Send(new CreateProductCommand(
            ProductName.Create("Widget"), Sku.Create("WIDGET01"), UnitPrice.Create(9.99m)));

        result.Should().BeFailureOfType<Error.Forbidden>();
    }

    [Fact]
    public async Task Cancel_ByOwner_Succeeds()
    {
        var fixture = new HandlerFixture();
        var customer = TestData.Customer();
        var product = TestData.Product(stock: 10);
        var order = TestData.OrderFor(customer, product, createdBy: "sales-1");
        fixture.Customers.Add(customer);
        fixture.Products.Add(product);
        fixture.Orders.Add(order);

        fixture.ActAs("sales-1", Permissions.OrdersCancel);
        var result = await fixture.Send(new CancelOrderCommand(order.Id));

        result.Should().BeSuccess();
        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public async Task Cancel_ByNonOwner_IsForbidden()
    {
        var fixture = new HandlerFixture();
        var customer = TestData.Customer();
        var product = TestData.Product(stock: 10);
        var order = TestData.OrderFor(customer, product, createdBy: "sales-1");
        fixture.Customers.Add(customer);
        fixture.Products.Add(product);
        fixture.Orders.Add(order);

        fixture.ActAs("sales-2", Permissions.OrdersCancel);
        var result = await fixture.Send(new CancelOrderCommand(order.Id));

        result.Should().BeFailureOfType<Error.Forbidden>();
        order.Status.Should().Be(OrderStatus.Draft);
    }

    [Fact]
    public async Task Cancel_ByAdmin_Succeeds()
    {
        var fixture = new HandlerFixture();
        var customer = TestData.Customer();
        var product = TestData.Product(stock: 10);
        var order = TestData.OrderFor(customer, product, createdBy: "sales-1");
        fixture.Customers.Add(customer);
        fixture.Products.Add(product);
        fixture.Orders.Add(order);

        fixture.ActAs("admin-1", Permissions.OrdersCancel, Permissions.OrdersReadAll);
        var result = await fixture.Send(new CancelOrderCommand(order.Id));

        result.Should().BeSuccess();
    }
}
