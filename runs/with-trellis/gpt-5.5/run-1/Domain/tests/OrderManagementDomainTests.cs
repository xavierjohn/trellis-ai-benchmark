namespace Domain.Tests;

using FluentAssertions;
using OrderManagement.Domain;
using Trellis.Testing;
using Xunit;

public sealed class OrderManagementDomainTests
{
    [Fact]
    public void Customer_validates_required_fields_and_optional_phone()
    {
        Customer.TryCreate("Jane", "Doe", "jane@example.com", null, Address())
            .Should().BeSuccess();

        Customer.TryCreate("", "Doe", "bad", "abc", Address())
            .Should().BeFailureOfType<Trellis.Error.InvalidInput>();
    }

    [Fact]
    public void Product_manages_stock_and_rejects_insufficient_reserve()
    {
        var product = Product.TryCreate("Widget", "ABC123", 10m).Unwrap();

        product.AddStock(5).Should().BeSuccess();
        product.ReserveStock(3).Should().BeSuccess();
        product.StockQuantity.Should().Be(2);
        product.ReserveStock(3).Should().BeFailureOfType<Trellis.Error.InvalidInput>();
    }

    [Fact]
    public void Order_enforces_line_item_rules_and_totals()
    {
        var product = Product.TryCreate("Widget", "ABC123", 10m).Unwrap();
        var second = Product.TryCreate("Gadget", "DEF456", 7m).Unwrap();
        var order = Order.TryCreate(Guid.NewGuid(), "actor-1", [(product, 2)], TimeProvider.System).Unwrap();

        order.Total.Should().Be(20m);
        order.AddLineItem(product, 1).Should().BeFailureOfType<Trellis.Error.InvalidInput>();
        order.AddLineItem(second, 1).Should().BeSuccess();
        order.RemoveLineItem(order.LineItems[1].Id).Should().BeSuccess();
        order.RemoveLineItem(order.LineItems[0].Id).Should().BeFailureOfType<Trellis.Error.InvalidInput>();
    }

    [Fact]
    public void State_machine_reserves_and_releases_stock()
    {
        var product = Product.TryCreate("Widget", "ABC123", 10m).Unwrap();
        product.AddStock(5).Should().BeSuccess();
        var order = Order.TryCreate(Guid.NewGuid(), "actor-1", [(product, 2)], TimeProvider.System).Unwrap();
        var products = new Dictionary<Guid, Product> { [product.Id] = product };

        order.Approve(TimeProvider.System).Should().BeFailureOfType<Trellis.Error.InvalidInput>();
        order.Submit(products, TimeProvider.System).Should().BeSuccess();
        product.StockQuantity.Should().Be(3);
        order.Approve(TimeProvider.System).Should().BeSuccess();
        order.Cancel(products, TimeProvider.System).Should().BeSuccess();
        product.StockQuantity.Should().Be(5);
    }

    [Fact]
    public void Overdue_specification_matches_submitted_orders_older_than_seven_days()
    {
        var product = Product.TryCreate("Widget", "ABC123", 10m).Unwrap();
        product.AddStock(5).Should().BeSuccess();
        var fakeTime = new FixedTimeProvider(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));
        var order = Order.TryCreate(Guid.NewGuid(), "actor-1", [(product, 1)], fakeTime).Unwrap();
        order.Submit(new Dictionary<Guid, Product> { [product.Id] = product }, fakeTime).Should().BeSuccess();

        OverdueOrderSpecification.IsSatisfiedBy(order, new DateTime(2026, 6, 9, 0, 0, 0, DateTimeKind.Utc)).Should().BeTrue();
        order.Approve(fakeTime).Should().BeSuccess();
        OverdueOrderSpecification.IsSatisfiedBy(order, new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc)).Should().BeFalse();
    }

    private static ShippingAddressInput Address() => new("1 Main", "Seattle", "WA", "98101", "USA");

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
