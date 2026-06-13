using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Products;

namespace OrderManagement.Tests.Domain;

public class OrderStateMachineTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-06-12T00:00:00Z");

    private static Product Prod(int stock) => Product.Create("Widget", "WIDGET01", 9.99m, stock).Value;

    private static (Order Order, Product Product) DraftOrder(int stock = 10, int qty = 2)
    {
        var product = Prod(stock);
        var requests = new List<(Order.LineRequest, Product)> { (new Order.LineRequest(product.Id, qty), product) };
        return (Order.CreateDraft(Guid.NewGuid(), "actor-1", requests, Now).Value, product);
    }

    private static IReadOnlyDictionary<Guid, Product> Map(Product p) => new Dictionary<Guid, Product> { [p.Id] = p };

    [Fact]
    public void Submit_ReservesStockAndSetsTimestamp()
    {
        var (order, product) = DraftOrder(stock: 10, qty: 3);

        var result = order.Submit(Map(product), Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Submitted, order.Status);
        Assert.Equal(Now, order.SubmittedAt);
        Assert.Equal(7, product.StockQuantity);
        Assert.Contains(order.DomainEvents, e => e is OrderSubmittedEvent);
    }

    [Fact]
    public void Submit_WithInsufficientStock_FailsAndDoesNotReserve()
    {
        var (order, product) = DraftOrder(stock: 2, qty: 3);

        var result = order.Submit(Map(product), Now);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderStatus.Draft, order.Status);
        Assert.Equal(2, product.StockQuantity);
    }

    [Fact]
    public void FullHappyPath_DraftToDelivered()
    {
        var (order, product) = DraftOrder();

        Assert.True(order.Submit(Map(product), Now).IsSuccess);
        Assert.True(order.Approve(Now).IsSuccess);
        Assert.True(order.Ship(Now.AddDays(1)).IsSuccess);
        Assert.Equal(Now.AddDays(1), order.ShippedAt);
        Assert.True(order.Deliver(Now.AddDays(2)).IsSuccess);
        Assert.Equal(OrderStatus.Delivered, order.Status);
    }

    [Fact]
    public void Approve_FromDraft_IsInvalidTransition()
    {
        var (order, _) = DraftOrder();

        var result = order.Approve(Now);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderStatus.Draft, order.Status);
    }

    [Fact]
    public void Ship_FromSubmitted_IsInvalidTransition()
    {
        var (order, product) = DraftOrder();
        order.Submit(Map(product), Now);

        var result = order.Ship(Now);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Deliver_FromApproved_IsInvalidTransition()
    {
        var (order, product) = DraftOrder();
        order.Submit(Map(product), Now);
        order.Approve(Now);

        var result = order.Deliver(Now);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Cancel_FromDraft_DoesNotReleaseStock()
    {
        var (order, product) = DraftOrder(stock: 10, qty: 3);

        var result = order.Cancel(Map(product), Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(10, product.StockQuantity); // never reserved
    }

    [Fact]
    public void Cancel_FromSubmitted_ReleasesStock()
    {
        var (order, product) = DraftOrder(stock: 10, qty: 3);
        order.Submit(Map(product), Now);
        Assert.Equal(7, product.StockQuantity);

        var result = order.Cancel(Map(product), Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(10, product.StockQuantity);
        Assert.Contains(order.DomainEvents, e => e is OrderCancelledEvent);
    }

    [Fact]
    public void Cancel_FromApproved_ReleasesStock()
    {
        var (order, product) = DraftOrder(stock: 10, qty: 3);
        order.Submit(Map(product), Now);
        order.Approve(Now);

        var result = order.Cancel(Map(product), Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(10, product.StockQuantity);
    }

    [Fact]
    public void Cancel_FromShipped_IsInvalidTransition()
    {
        var (order, product) = DraftOrder();
        order.Submit(Map(product), Now);
        order.Approve(Now);
        order.Ship(Now);

        var result = order.Cancel(Map(product), Now);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderStatus.Shipped, order.Status);
    }

    [Fact]
    public void Cancel_FromDelivered_IsInvalidTransition()
    {
        var (order, product) = DraftOrder();
        order.Submit(Map(product), Now);
        order.Approve(Now);
        order.Ship(Now);
        order.Deliver(Now);

        var result = order.Cancel(Map(product), Now);

        Assert.True(result.IsFailure);
    }
}
