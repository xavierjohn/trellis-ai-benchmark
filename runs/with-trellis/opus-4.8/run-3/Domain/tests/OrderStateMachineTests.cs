namespace Domain.Tests;

using OrderManagement.Domain;

internal sealed class FixedTimeProvider : TimeProvider
{
    private DateTimeOffset _now;
    public FixedTimeProvider(DateTimeOffset now) => _now = now;
    public override DateTimeOffset GetUtcNow() => _now;
    public void Advance(TimeSpan by) => _now += by;
}

public class OrderStateMachineTests
{
    private static readonly TimeProvider Time = new FixedTimeProvider(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));

    private static (Order order, Product product, Dictionary<ProductId, Product> products) Setup(int stock = 100, int qty = 2)
    {
        var product = TestData.Product(stock: stock);
        var order = TestData.DraftOrder(product, quantity: qty);
        var products = new Dictionary<ProductId, Product> { [product.Id] = product };
        return (order, product, products);
    }

    [Fact]
    public void Full_lifecycle_succeeds()
    {
        var (order, _, products) = Setup();

        order.Submit(products, Time).Should().BeSuccess();
        order.Status.Should().Be(OrderStatus.Submitted);
        order.SubmittedAt.Should().HaveValue();

        order.Approve(Time).Should().BeSuccess();
        order.Status.Should().Be(OrderStatus.Approved);

        order.Ship(Time).Should().BeSuccess();
        order.Status.Should().Be(OrderStatus.Shipped);
        order.ShippedAt.Should().HaveValue();

        order.Deliver(Time).Should().BeSuccess();
        order.Status.Should().Be(OrderStatus.Delivered);
    }

    [Fact]
    public void Submit_reserves_stock()
    {
        var (order, product, products) = Setup(stock: 100, qty: 3);

        order.Submit(products, Time).Should().BeSuccess();

        product.StockQuantity.Value.Should().Be(97);
    }

    [Fact]
    public void Submit_with_insufficient_stock_fails()
    {
        var (order, product, products) = Setup(stock: 1, qty: 5);

        order.Submit(products, Time).Should().BeFailureOfType<Error.InvalidInput>();
        order.Status.Should().Be(OrderStatus.Draft);
        product.StockQuantity.Value.Should().Be(1);
    }

    [Fact]
    public void Cancel_from_draft_does_not_release_stock()
    {
        var (order, product, _) = Setup();
        var products = new Dictionary<ProductId, Product> { [product.Id] = product };

        order.Cancel(products, Time).Should().BeSuccess();

        order.Status.Should().Be(OrderStatus.Cancelled);
        product.StockQuantity.Value.Should().Be(100);
    }

    [Fact]
    public void Cancel_from_submitted_releases_stock()
    {
        var (order, product, products) = Setup(stock: 100, qty: 4);
        order.Submit(products, Time).Unwrap();
        product.StockQuantity.Value.Should().Be(96);

        order.Cancel(products, Time).Should().BeSuccess();

        order.Status.Should().Be(OrderStatus.Cancelled);
        product.StockQuantity.Value.Should().Be(100);
    }

    [Fact]
    public void Cancel_from_approved_releases_stock()
    {
        var (order, product, products) = Setup(stock: 100, qty: 4);
        order.Submit(products, Time).Unwrap();
        order.Approve(Time).Unwrap();

        order.Cancel(products, Time).Should().BeSuccess();

        product.StockQuantity.Value.Should().Be(100);
    }

    [Fact]
    public void Approve_from_draft_is_invalid()
    {
        var (order, _, _) = Setup();

        order.Approve(Time).Should().BeFailureOfType<Error.InvalidInput>();
        order.Status.Should().Be(OrderStatus.Draft);
    }

    [Fact]
    public void Ship_from_submitted_is_invalid()
    {
        var (order, _, products) = Setup();
        order.Submit(products, Time).Unwrap();

        order.Ship(Time).Should().BeFailureOfType<Error.InvalidInput>();
    }

    [Fact]
    public void Deliver_from_approved_is_invalid()
    {
        var (order, _, products) = Setup();
        order.Submit(products, Time).Unwrap();
        order.Approve(Time).Unwrap();

        order.Deliver(Time).Should().BeFailureOfType<Error.InvalidInput>();
    }

    [Fact]
    public void Cancel_from_shipped_is_invalid()
    {
        var (order, _, products) = Setup();
        order.Submit(products, Time).Unwrap();
        order.Approve(Time).Unwrap();
        order.Ship(Time).Unwrap();

        order.Cancel(products, Time).Should().BeFailureOfType<Error.InvalidInput>();
        order.Status.Should().Be(OrderStatus.Shipped);
    }

    [Fact]
    public void Submit_from_submitted_is_invalid()
    {
        var (order, _, products) = Setup();
        order.Submit(products, Time).Unwrap();

        order.Submit(products, Time).Should().BeFailureOfType<Error.InvalidInput>();
    }
}
