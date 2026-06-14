namespace Domain.Tests;

using OrderManagement.Domain;
using Trellis.Primitives;

public sealed class OrderManagementDomainTests
{
    [Fact]
    public void Customer_creation_accepts_optional_phone()
    {
        var customer = new Customer(
            FirstName.Create("Ada"),
            LastName.Create("Lovelace"),
            EmailAddress.Create("ada@example.com"),
            Maybe<PhoneNumber>.None,
            Address());

        customer.PhoneNumber.Should().BeNone();
        customer.Email.Value.Should().Be("ada@example.com");
    }

    [Fact]
    public void Product_stock_rules_reserve_and_reject_insufficient_stock()
    {
        var product = Product();

        product.AddStock(StockAdjustmentQuantity.Create(5)).Should().BeSuccess();
        product.ReserveStock(LineItemQuantity.Create(3)).Should().BeSuccess();
        product.StockQuantity.Value.Should().Be(2);
        product.ReserveStock(LineItemQuantity.Create(3)).Should().BeFailureOfType<Error.InvalidInput>();
    }

    [Fact]
    public void Order_line_item_rules_reject_duplicates_and_last_removal()
    {
        var product = Product();
        var order = Order.TryCreate(
            CustomerId.NewUniqueV7(),
            [(product, LineItemQuantity.Create(1))],
            "actor-1",
            TimeProvider.System).Unwrap();

        order.AddLineItem(product, LineItemQuantity.Create(1)).Should().BeFailureOfType<Error.InvalidInput>();
        order.RemoveLineItem(order.LineItems.Single().Id).Should().BeFailureOfType<Error.InvalidInput>();
    }

    [Fact]
    public void Submit_reserves_stock_and_cancel_releases_it()
    {
        var time = new FixedTimeProvider(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));
        var product = Product();
        product.AddStock(StockAdjustmentQuantity.Create(10)).Should().BeSuccess();
        var order = Order.TryCreate(
            CustomerId.NewUniqueV7(),
            [(product, LineItemQuantity.Create(4))],
            "actor-1",
            time).Unwrap();

        order.Submit(new Dictionary<ProductId, Product> { [product.Id] = product }, time).Should().BeSuccess();
        product.StockQuantity.Value.Should().Be(6);
        order.Cancel(new Dictionary<ProductId, Product> { [product.Id] = product }, time).Should().BeSuccess();
        product.StockQuantity.Value.Should().Be(10);
    }

    [Fact]
    public void State_machine_allows_valid_lifecycle_and_rejects_invalid_transition()
    {
        var time = new FixedTimeProvider(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));
        var product = Product();
        product.AddStock(StockAdjustmentQuantity.Create(10)).Should().BeSuccess();
        var products = new Dictionary<ProductId, Product> { [product.Id] = product };
        var order = Order.TryCreate(CustomerId.NewUniqueV7(), [(product, LineItemQuantity.Create(1))], "actor-1", time).Unwrap();

        order.Approve(time).Should().BeFailureOfType<Error.InvalidInput>();
        order.Submit(products, time).Should().BeSuccess();
        order.Approve(time).Should().BeSuccess();
        order.Ship(time).Should().BeSuccess();
        order.Deliver(time).Should().BeSuccess();
        order.Cancel(products, time).Should().BeFailureOfType<Error.InvalidInput>();
    }

    [Fact]
    public void Overdue_specification_matches_submitted_orders_older_than_seven_days()
    {
        var oldTime = new FixedTimeProvider(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));
        var recentTime = new FixedTimeProvider(new DateTimeOffset(2026, 6, 8, 0, 0, 0, TimeSpan.Zero));
        var product = Product();
        product.AddStock(StockAdjustmentQuantity.Create(10)).Should().BeSuccess();
        var products = new Dictionary<ProductId, Product> { [product.Id] = product };
        var old = Order.TryCreate(CustomerId.NewUniqueV7(), [(product, LineItemQuantity.Create(1))], "actor-1", oldTime).Unwrap();
        old.Submit(products, oldTime).Should().BeSuccess();
        var recent = Order.TryCreate(CustomerId.NewUniqueV7(), [(product, LineItemQuantity.Create(1))], "actor-1", recentTime).Unwrap();
        recent.Submit(products, recentTime).Should().BeSuccess();
        var approved = Order.TryCreate(CustomerId.NewUniqueV7(), [(product, LineItemQuantity.Create(1))], "actor-1", oldTime).Unwrap();
        approved.Submit(products, oldTime).Should().BeSuccess();
        approved.Approve(oldTime).Should().BeSuccess();
        var spec = new OverdueOrderSpecification(new DateTime(2026, 6, 9, 0, 0, 0, DateTimeKind.Utc).AddDays(-7));

        spec.IsSatisfiedBy(old).Should().BeTrue();
        spec.IsSatisfiedBy(recent).Should().BeFalse();
        spec.IsSatisfiedBy(approved).Should().BeFalse();
    }

    private static ShippingAddress Address() => new(
        Street.Create("1 Main St"),
        City.Create("Seattle"),
        StateProvince.Create("WA"),
        PostalCode.Create("98101"),
        Country.Create("USA"));

    private static Product Product(string sku = "ABC123") =>
        OrderManagement.Domain.Product.TryCreate(ProductName.Create("Widget"), Sku.Create(sku), MonetaryAmount.Create(10m)).Unwrap();

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
