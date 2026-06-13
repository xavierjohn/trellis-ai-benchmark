namespace Domain.Tests.Order;

using OrderManagement.Domain;
using Trellis.Primitives;
using Trellis.Testing;

public sealed class DomainBehaviorTests
{
    [Fact]
    public void Product_reserve_stock_rejects_insufficient_stock()
    {
        var product = Product();
        product.AddStock(OrderQuantity.Create(2)).Should().BeSuccess();

        product.ReserveStock(OrderQuantity.Create(3)).Should().BeFailureOfType<Error.InvalidInput>();
        product.StockQuantity.Value.Should().Be(2);
    }

    [Fact]
    public void Order_rejects_duplicate_products()
    {
        var product = Product();
        var result = OrderManagement.Domain.Order.TryCreate(
            CustomerId.NewUniqueV7(),
            [(product, OrderQuantity.Create(1)), (product, OrderQuantity.Create(2))],
            "actor-1",
            TimeProvider.System);

        result.Should().BeFailureOfType<Error.InvalidInput>();
    }

    [Fact]
    public void Submit_reserves_stock_and_cancel_releases_it()
    {
        var product = Product();
        product.AddStock(OrderQuantity.Create(5)).Should().BeSuccess();
        var order = OrderManagement.Domain.Order.TryCreate(
            CustomerId.NewUniqueV7(),
            [(product, OrderQuantity.Create(3))],
            "actor-1",
            TimeProvider.System).Unwrap();
        var products = new Dictionary<ProductId, Product> { [product.Id] = product };

        order.Submit(products, TimeProvider.System).Should().BeSuccess();
        product.StockQuantity.Value.Should().Be(2);
        order.Status.Should().Be(OrderStatus.Submitted);

        order.Cancel(products, TimeProvider.System).Should().BeSuccess();
        product.StockQuantity.Value.Should().Be(5);
        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public void Invalid_transition_returns_validation_error()
    {
        var product = Product();
        var order = OrderManagement.Domain.Order.TryCreate(
            CustomerId.NewUniqueV7(),
            [(product, OrderQuantity.Create(1))],
            "actor-1",
            TimeProvider.System).Unwrap();

        order.Approve(TimeProvider.System).Should().BeFailureOfType<Error.InvalidInput>();
    }

    [Fact]
    public void Overdue_specification_matches_old_submitted_orders_only()
    {
        var product = Product();
        product.AddStock(OrderQuantity.Create(3)).Should().BeSuccess();
        var order = OrderManagement.Domain.Order.TryCreate(
            CustomerId.NewUniqueV7(),
            [(product, OrderQuantity.Create(1))],
            "actor-1",
            TimeProvider.System).Unwrap();
        order.Submit(new Dictionary<ProductId, Product> { [product.Id] = product }, TimeProvider.System).Should().BeSuccess();

        var spec = new OverdueOrderSpecification(DateTime.UtcNow.AddDays(8));
        spec.ToExpression().Compile()(order).Should().BeTrue();

        order.Approve(TimeProvider.System).Should().BeSuccess();
        spec.ToExpression().Compile()(order).Should().BeFalse();
    }

    private static Product Product() =>
        new(ProductName.Create("Widget"), Sku.Create($"SKU{Guid.NewGuid():N}"[..10].ToUpperInvariant()), UnitPrice.Create(10m));
}
