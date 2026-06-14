using Microsoft.Extensions.Time.Testing;
using OrderManagement.Api.Domain.Common;
using OrderManagement.Api.Domain.Orders;
using OrderManagement.Api.Domain.Products;
using Xunit;

namespace OrderManagement.Tests.Domain;

public class OrderStateMachineTests
{
    private static FakeTimeProvider NewTime() =>
        new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

    private static (Order order, Guid productId, Dictionary<Guid, Product> products) BuildOrderWithStock(
        int stock, int qty, FakeTimeProvider time)
    {
        var product = Product.Create("Widget", "WIDGET01", 10m, stock);
        var drafts = new List<LineItemDraft> { new(product.Id, "Widget", 10m, qty) };
        var order = Order.CreateDraft(Guid.NewGuid(), "actor-1", drafts, time);
        var map = new Dictionary<Guid, Product> { [product.Id] = product };
        return (order, product.Id, map);
    }

    [Fact]
    public void Submit_ReservesStock_AndSetsSubmittedAt()
    {
        var time = NewTime();
        var (order, pid, products) = BuildOrderWithStock(stock: 10, qty: 3, time);

        order.Submit(products, time);

        Assert.Equal(OrderStatus.Submitted, order.Status);
        Assert.Equal(7, products[pid].StockQuantity);
        Assert.Equal(time.GetUtcNow().UtcDateTime, order.SubmittedAt);
        Assert.Contains(order.Events, e => e is OrderSubmittedEvent);
    }

    [Fact]
    public void Submit_WithInsufficientStock_Throws_AndDoesNotReserve()
    {
        var time = NewTime();
        var (order, pid, products) = BuildOrderWithStock(stock: 2, qty: 3, time);

        Assert.Throws<ValidationAppException>(() => order.Submit(products, time));
        Assert.Equal(OrderStatus.Draft, order.Status);
        Assert.Equal(2, products[pid].StockQuantity);
    }

    [Fact]
    public void FullHappyPath_DraftToDelivered()
    {
        var time = NewTime();
        var (order, _, products) = BuildOrderWithStock(stock: 10, qty: 1, time);

        order.Submit(products, time);
        order.Approve(time);
        order.Ship(time);
        order.Deliver(time);

        Assert.Equal(OrderStatus.Delivered, order.Status);
        Assert.NotNull(order.ShippedAt);
    }

    [Fact]
    public void Cancel_FromSubmitted_ReleasesStock()
    {
        var time = NewTime();
        var (order, pid, products) = BuildOrderWithStock(stock: 10, qty: 4, time);

        order.Submit(products, time);
        Assert.Equal(6, products[pid].StockQuantity);

        order.Cancel(products, time);

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(10, products[pid].StockQuantity);
    }

    [Fact]
    public void Cancel_FromDraft_DoesNotChangeStock()
    {
        var time = NewTime();
        var (order, pid, products) = BuildOrderWithStock(stock: 10, qty: 4, time);

        order.Cancel(products, time);

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(10, products[pid].StockQuantity);
    }

    [Fact]
    public void Cancel_FromApproved_ReleasesStock()
    {
        var time = NewTime();
        var (order, pid, products) = BuildOrderWithStock(stock: 10, qty: 4, time);
        order.Submit(products, time);
        order.Approve(time);

        order.Cancel(products, time);

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(10, products[pid].StockQuantity);
    }

    // ----- Invalid transitions -----

    [Fact]
    public void Approve_FromDraft_Throws()
    {
        var time = NewTime();
        var (order, _, _) = BuildOrderWithStock(10, 1, time);
        Assert.Throws<ValidationAppException>(() => order.Approve(time));
    }

    [Fact]
    public void Ship_FromSubmitted_Throws()
    {
        var time = NewTime();
        var (order, _, products) = BuildOrderWithStock(10, 1, time);
        order.Submit(products, time);
        Assert.Throws<ValidationAppException>(() => order.Ship(time));
    }

    [Fact]
    public void Deliver_FromApproved_Throws()
    {
        var time = NewTime();
        var (order, _, products) = BuildOrderWithStock(10, 1, time);
        order.Submit(products, time);
        order.Approve(time);
        Assert.Throws<ValidationAppException>(() => order.Deliver(time));
    }

    [Fact]
    public void Cancel_FromShipped_Throws()
    {
        var time = NewTime();
        var (order, _, products) = BuildOrderWithStock(10, 1, time);
        order.Submit(products, time);
        order.Approve(time);
        order.Ship(time);
        Assert.Throws<ValidationAppException>(() => order.Cancel(products, time));
    }

    [Fact]
    public void Cancel_FromDelivered_Throws()
    {
        var time = NewTime();
        var (order, _, products) = BuildOrderWithStock(10, 1, time);
        order.Submit(products, time);
        order.Approve(time);
        order.Ship(time);
        order.Deliver(time);
        Assert.Throws<ValidationAppException>(() => order.Cancel(products, time));
    }

    [Fact]
    public void Submit_FromSubmitted_Throws()
    {
        var time = NewTime();
        var (order, _, products) = BuildOrderWithStock(10, 1, time);
        order.Submit(products, time);
        Assert.Throws<ValidationAppException>(() => order.Submit(products, time));
    }
}
