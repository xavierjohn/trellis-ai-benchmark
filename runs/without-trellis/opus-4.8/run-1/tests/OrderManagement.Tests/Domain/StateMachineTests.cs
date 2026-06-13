using OrderManagement.Domain.Common;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Products;
using static OrderManagement.Tests.Domain.DomainFixtures;

namespace OrderManagement.Tests.Domain;

public class StateMachineTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private readonly Guid _customer = Guid.NewGuid();

    private (Order order, Product product) DraftWithStock(int stock = 10, int qty = 3)
    {
        var p = ProductWithStock("SKU001", 10m, stock);
        var order = DraftOrder(_customer, "actor-1", (p, qty));
        return (order, p);
    }

    [Fact]
    public void Submit_ReservesStock_AndSetsSubmittedAt()
    {
        var (order, p) = DraftWithStock(stock: 10, qty: 3);

        var result = order.Submit(Dict(p), T0);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Submitted, order.Status);
        Assert.Equal(T0, order.SubmittedAt);
        Assert.Equal(7, p.StockQuantity);
        Assert.Contains(order.DomainEvents, e => e is OrderSubmittedEvent);
    }

    [Fact]
    public void Submit_WithInsufficientStock_Fails_AndDoesNotReserve()
    {
        var (order, p) = DraftWithStock(stock: 2, qty: 3);

        var result = order.Submit(Dict(p), T0);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
        Assert.Equal(2, p.StockQuantity);
        Assert.Equal(OrderStatus.Draft, order.Status);
    }

    [Fact]
    public void FullHappyPath_DraftToDelivered()
    {
        var (order, p) = DraftWithStock();

        Assert.True(order.Submit(Dict(p), T0).IsSuccess);
        Assert.True(order.Approve(T0).IsSuccess);
        Assert.True(order.Ship(T0).IsSuccess);
        Assert.Equal(T0, order.ShippedAt);
        Assert.True(order.Deliver(T0).IsSuccess);
        Assert.Equal(OrderStatus.Delivered, order.Status);
    }

    [Fact]
    public void Cancel_FromDraft_DoesNotTouchStock()
    {
        var (order, p) = DraftWithStock(stock: 10, qty: 3);

        var result = order.Cancel(Dict(p), T0);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(10, p.StockQuantity);
    }

    [Fact]
    public void Cancel_FromSubmitted_ReleasesStock()
    {
        var (order, p) = DraftWithStock(stock: 10, qty: 3);
        order.Submit(Dict(p), T0);
        Assert.Equal(7, p.StockQuantity);

        var result = order.Cancel(Dict(p), T0);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(10, p.StockQuantity);
    }

    [Fact]
    public void Cancel_FromApproved_ReleasesStock()
    {
        var (order, p) = DraftWithStock(stock: 10, qty: 3);
        order.Submit(Dict(p), T0);
        order.Approve(T0);

        order.Cancel(Dict(p), T0);

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(10, p.StockQuantity);
    }

    [Fact]
    public void Cancel_FromShipped_Fails()
    {
        var (order, p) = DraftWithStock();
        order.Submit(Dict(p), T0);
        order.Approve(T0);
        order.Ship(T0);

        var result = order.Cancel(Dict(p), T0);
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
    }

    [Fact]
    public void Cancel_FromDelivered_Fails()
    {
        var (order, p) = DraftWithStock();
        order.Submit(Dict(p), T0);
        order.Approve(T0);
        order.Ship(T0);
        order.Deliver(T0);

        Assert.True(order.Cancel(Dict(p), T0).IsFailure);
    }

    [Theory]
    [InlineData(OrderStatus.Draft)]      // Approve from Draft invalid
    public void Approve_FromInvalidState_Fails(OrderStatus _)
    {
        var (order, _p) = DraftWithStock();
        var result = order.Approve(T0); // still Draft
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
    }

    [Fact]
    public void Ship_FromSubmitted_Fails()
    {
        var (order, p) = DraftWithStock();
        order.Submit(Dict(p), T0);
        Assert.True(order.Ship(T0).IsFailure); // must be Approved
    }

    [Fact]
    public void Deliver_FromApproved_Fails()
    {
        var (order, p) = DraftWithStock();
        order.Submit(Dict(p), T0);
        order.Approve(T0);
        Assert.True(order.Deliver(T0).IsFailure); // must be Shipped
    }

    [Fact]
    public void Submit_FromSubmitted_Fails()
    {
        var (order, p) = DraftWithStock();
        order.Submit(Dict(p), T0);
        Assert.True(order.Submit(Dict(p), T0).IsFailure);
    }
}
