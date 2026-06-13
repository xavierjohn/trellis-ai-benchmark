namespace OrderManagement.Domain.Tests;

using OrderManagement.Domain;
using Trellis.Authorization;
using Trellis.Primitives;

public class DomainBehaviorTests
{
    [Fact]
    public void Customer_accepts_optional_phone_and_rejects_invalid_email()
    {
        EmailAddress.TryCreate("bad", "email").Should().BeFailureOfType<Error.InvalidInput>();

        var customer = new Customer(
            FirstName.Create("Ada"),
            LastName.Create("Lovelace"),
            EmailAddress.Create("ada@example.com"),
            Maybe<PhoneNumber>.None,
            ShippingAddress.TryCreate("1 Main", "London", "LDN", "SW1", "UK").Unwrap(),
            TimeProvider.System);

        customer.PhoneNumber.Should().BeNone();
    }

    [Fact]
    public void Product_adds_and_reserves_stock_without_going_negative()
    {
        var product = Product();
        product.AddStock(StockAdjustmentQuantity.Create(5)).Should().BeSuccess();
        product.ReserveStock(StockAdjustmentQuantity.Create(3)).Should().BeSuccess();
        product.StockQuantity.Value.Should().Be(2);

        product.ReserveStock(StockAdjustmentQuantity.Create(3)).Should().BeFailureOfType<Error.InvalidInput>();
    }

    [Fact]
    public void Order_enforces_line_item_rules_and_transitions()
    {
        var product = Product();
        var order = Order.TryCreate(
            Customer(),
            ActorId.Create("actor-1"),
            [(product, LineItemQuantity.Create(2))],
            TimeProvider.System).Unwrap();

        order.Status.Should().Be(OrderStatus.Draft);
        order.AddLineItem(product, LineItemQuantity.Create(1), TimeProvider.System).Should().BeFailureOfType<Error.InvalidInput>();
        order.RemoveLineItem(order.LineItems.Single().Id).Should().BeFailureOfType<Error.InvalidInput>();

        order.Submit(TimeProvider.System).Should().BeSuccess();
        order.Approve(TimeProvider.System).Should().BeSuccess();
        order.Ship(TimeProvider.System).Should().BeSuccess();
        order.Deliver(TimeProvider.System).Should().BeSuccess();
        order.Cancel(TimeProvider.System).Should().BeFailureOfType<Error.InvalidInput>();
    }

    [Fact]
    public void Overdue_specification_matches_old_submitted_orders_only()
    {
        var old = Order.TryCreate(Customer(), ActorId.Create("actor-1"), [(Product(), LineItemQuantity.Create(1))], TimeProvider.System).Unwrap();
        old.Submit(TimeProvider.System).Should().BeSuccess();
        old.SetMaybeField(o => o.SubmittedAt, DateTime.UtcNow.AddDays(-8));

        var recent = Order.TryCreate(Customer(), ActorId.Create("actor-1"), [(Product(), LineItemQuantity.Create(1))], TimeProvider.System).Unwrap();
        recent.Submit(TimeProvider.System).Should().BeSuccess();

        var predicate = new OverdueOrderSpecification(DateTime.UtcNow).ToExpression().Compile();
        predicate(old).Should().BeTrue();
        predicate(recent).Should().BeFalse();
    }

    private static Customer Customer() =>
        new(
            FirstName.Create("Grace"),
            LastName.Create("Hopper"),
            EmailAddress.Create($"{Guid.NewGuid():N}@example.com"),
            Maybe<PhoneNumber>.None,
            ShippingAddress.TryCreate("1 Main", "Arlington", "VA", "22201", "USA").Unwrap(),
            TimeProvider.System);

    private static Product Product() =>
        new(ProductName.Create("Widget"), Sku.Create(Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()), UnitPrice.Create(10m), TimeProvider.System);
}
