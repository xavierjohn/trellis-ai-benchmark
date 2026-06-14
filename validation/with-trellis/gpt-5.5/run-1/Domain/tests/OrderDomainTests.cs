namespace OrderManagement.Domain.Tests;

using OrderManagement.Domain;
using Trellis.Authorization;
using Trellis.Primitives;

public sealed class OrderDomainTests
{
    [Fact]
    public void Customer_validates_email_and_optional_phone()
    {
        var address = ShippingAddress.TryCreate("1 Main", "Seattle", "WA", "98101", "US").Unwrap();
        var customer = Customer.TryCreate(
            FirstName.Create("Ada"),
            LastName.Create("Lovelace"),
            EmailAddress.TryCreate("ada@example.com").Unwrap(),
            Maybe<PhoneNumber>.None,
            address);

        customer.Should().BeSuccess();
        EmailAddress.TryCreate("not-email").Should().BeFailureOfType<Error.InvalidInput>();
    }

    [Fact]
    public void Product_reserves_and_releases_stock()
    {
        var product = Product.TryCreate(ProductName.Create("Widget"), Sku.Create("ABC123"), UnitPrice.Create(12.5m)).Unwrap();

        product.AddStock(StockAdjustmentQuantity.Create(5)).Should().BeSuccess();
        product.ReserveStock(OrderQuantity.Create(3)).Should().BeSuccess();
        product.StockQuantity.Value.Should().Be(2);
        product.ReserveStock(OrderQuantity.Create(3)).Should().BeFailureOfType<Error.InvalidInput>();
        product.ReleaseStock(OrderQuantity.Create(3)).Should().BeSuccess();
        product.StockQuantity.Value.Should().Be(5);
    }

    [Fact]
    public void Order_enforces_line_item_rules_and_transitions()
    {
        var product = Product.TryCreate(ProductName.Create("Widget"), Sku.Create("ABC123"), UnitPrice.Create(10m)).Unwrap();
        var order = Order.TryCreate(
            CustomerId.NewUniqueV7(),
            ActorId.Create("actor-1"),
            [(product, OrderQuantity.Create(2))]).Unwrap();

        order.Total.Should().Be(20m);
        order.AddLineItem(product, OrderQuantity.Create(1)).Should().BeFailureOfType<Error.InvalidInput>();
        order.RemoveLineItem(order.LineItems.Single().Id).Should().BeFailureOfType<Error.InvalidInput>();

        order.Submit(TimeProvider.System).Should().BeSuccess();
        order.Approve(TimeProvider.System).Should().BeSuccess();
        order.Ship(TimeProvider.System).Should().BeSuccess();
        order.Deliver(TimeProvider.System).Should().BeSuccess();
        order.Cancel(TimeProvider.System).Should().BeFailureOfType<Error.InvalidInput>();
    }
}
