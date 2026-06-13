using OrderManagement.Api;

namespace OrderManagement.Tests;

public sealed class DomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 12, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Customer_creation_validates_required_fields_and_optional_phone()
    {
        var customer = new Customer("Ada", "Lovelace", "ADA@example.com", null, Address());

        Assert.Equal("ada@example.com", customer.Email);
        Assert.Null(customer.PhoneNumber);
        Assert.Throws<DomainException>(() => new Customer("", "Lovelace", "ada@example.com", null, Address()));
        Assert.Throws<DomainException>(() => new Customer("Ada", "Lovelace", "not-email", null, Address()));
    }

    [Fact]
    public void Product_adds_and_reserves_stock_without_going_negative()
    {
        var product = new Product("Keyboard", "KEY123", 99.95m);

        product.AddStock(5);
        product.ReserveStock(3);

        Assert.Equal(2, product.StockQuantity);
        Assert.Throws<DomainException>(() => product.ReserveStock(3));
        Assert.Throws<DomainException>(() => product.AddStock(0));
    }

    [Fact]
    public void Order_line_items_enforce_duplicates_and_last_line_protection()
    {
        var first = Item(Guid.NewGuid(), 2);
        var order = new Order(Guid.NewGuid(), "actor-1", [first], Now);
        var second = Item(Guid.NewGuid(), 1);

        order.AddLineItem(second);
        Assert.Equal(2, order.LineItems.Count);
        Assert.Throws<DomainException>(() => order.AddLineItem(Item(first.ProductId, 1)));

        order.RemoveLineItem(second.Id);
        Assert.Single(order.LineItems);
        Assert.Throws<DomainException>(() => order.RemoveLineItem(first.Id));
    }

    [Fact]
    public void State_machine_allows_valid_transitions_and_reserves_stock()
    {
        var product = new Product("Keyboard", "KEY123", 10m);
        product.AddStock(10);
        var order = new Order(Guid.NewGuid(), "actor-1", [new OrderLineItem(product.Id, product.ProductName, 3, product.UnitPrice)], Now);

        order.Submit([product], Now.AddMinutes(1));
        order.Approve(Now.AddMinutes(2));
        order.Ship(Now.AddMinutes(3));
        order.Deliver(Now.AddMinutes(4));

        Assert.Equal(OrderStatus.Delivered, order.Status);
        Assert.Equal(7, product.StockQuantity);
        Assert.Equal(30m, order.Total);
        Assert.Equal(Now.AddMinutes(1), order.SubmittedAt);
        Assert.Equal(Now.AddMinutes(3), order.ShippedAt);
    }

    [Fact]
    public void State_machine_rejects_invalid_transitions()
    {
        var product = new Product("Keyboard", "KEY123", 10m);
        product.AddStock(10);
        var order = new Order(Guid.NewGuid(), "actor-1", [new OrderLineItem(product.Id, product.ProductName, 1, product.UnitPrice)], Now);

        Assert.Throws<DomainException>(() => order.Approve(Now));
        order.Submit([product], Now);
        Assert.Throws<DomainException>(() => order.Ship(Now));
        order.Approve(Now);
        order.Ship(Now);
        Assert.Throws<DomainException>(() => order.Cancel([product], Now));
    }

    [Fact]
    public void Cancel_releases_reserved_stock_for_submitted_or_approved_orders()
    {
        var product = new Product("Keyboard", "KEY123", 10m);
        product.AddStock(5);
        var order = new Order(Guid.NewGuid(), "actor-1", [new OrderLineItem(product.Id, product.ProductName, 4, product.UnitPrice)], Now);

        order.Submit([product], Now);
        Assert.Equal(1, product.StockQuantity);
        order.Cancel([product], Now.AddMinutes(1));

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(5, product.StockQuantity);
    }

    [Fact]
    public void Overdue_spec_matches_only_old_submitted_orders()
    {
        var oldSubmitted = SubmittedOrder(Now.AddDays(-8));
        var recentSubmitted = SubmittedOrder(Now.AddDays(-2));
        var approved = SubmittedOrder(Now.AddDays(-8));
        approved.Approve(Now.AddDays(-7));

        Assert.True(oldSubmitted.IsOverdue(Now));
        Assert.False(recentSubmitted.IsOverdue(Now));
        Assert.False(approved.IsOverdue(Now));
    }

    private static Order SubmittedOrder(DateTimeOffset submittedAt)
    {
        var product = new Product("Keyboard", $"K{submittedAt.Day:00}1", 10m);
        product.AddStock(10);
        var order = new Order(Guid.NewGuid(), "actor-1", [new OrderLineItem(product.Id, product.ProductName, 1, product.UnitPrice)], submittedAt.AddMinutes(-1));
        order.Submit([product], submittedAt);
        return order;
    }

    private static ShippingAddress Address() => new("1 Main", "Seattle", "WA", "98101", "USA");
    private static OrderLineItem Item(Guid productId, int quantity) => new(productId, "Keyboard", quantity, 10m);
}
