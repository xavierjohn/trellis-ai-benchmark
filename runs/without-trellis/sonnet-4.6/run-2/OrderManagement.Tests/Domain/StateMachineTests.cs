using OrderManagement.Api.Domain;
using Xunit;

namespace OrderManagement.Tests.Domain;

public class StateMachineTests
{
    private static readonly DateTime _baseTime = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly FakeTimeProvider _timeProvider = new FakeTimeProvider(_baseTime);

    private static Order CreateDraftOrderWithItem()
    {
        var customerId = Guid.NewGuid();
        var order = Order.Create(customerId, "actor-1", _baseTime);
        var product = Product.Create("Widget", "WGT001", 10.00m);
        var lineItem = LineItem.Create(order.Id, product.Id, product.ProductName, 5, product.UnitPrice);
        order.AddLineItem(lineItem);
        return order;
    }

    [Fact]
    public void Submit_FromDraft_TransitionsToSubmitted()
    {
        var order = CreateDraftOrderWithItem();

        order.Submit(_timeProvider);

        Assert.Equal(OrderStatus.Submitted, order.Status);
        Assert.NotNull(order.SubmittedAt);
        Assert.Equal(_baseTime, order.SubmittedAt!.Value);
    }

    [Fact]
    public void Approve_FromSubmitted_TransitionsToApproved()
    {
        var order = CreateDraftOrderWithItem();
        order.Submit(_timeProvider);

        order.Approve(_timeProvider);

        Assert.Equal(OrderStatus.Approved, order.Status);
    }

    [Fact]
    public void Ship_FromApproved_TransitionsToShipped()
    {
        var order = CreateDraftOrderWithItem();
        order.Submit(_timeProvider);
        order.Approve(_timeProvider);

        order.Ship(_timeProvider);

        Assert.Equal(OrderStatus.Shipped, order.Status);
        Assert.NotNull(order.ShippedAt);
    }

    [Fact]
    public void Deliver_FromShipped_TransitionsToDelivered()
    {
        var order = CreateDraftOrderWithItem();
        order.Submit(_timeProvider);
        order.Approve(_timeProvider);
        order.Ship(_timeProvider);

        order.Deliver(_timeProvider);

        Assert.Equal(OrderStatus.Delivered, order.Status);
    }

    [Fact]
    public void Cancel_FromDraft_TransitionsToCancelled()
    {
        var order = CreateDraftOrderWithItem();

        order.Cancel(_timeProvider);

        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void Cancel_FromSubmitted_TransitionsToCancelled()
    {
        var order = CreateDraftOrderWithItem();
        order.Submit(_timeProvider);

        order.Cancel(_timeProvider);

        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void Cancel_FromApproved_TransitionsToCancelled()
    {
        var order = CreateDraftOrderWithItem();
        order.Submit(_timeProvider);
        order.Approve(_timeProvider);

        order.Cancel(_timeProvider);

        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void Cancel_FromShipped_ThrowsValidationException()
    {
        var order = CreateDraftOrderWithItem();
        order.Submit(_timeProvider);
        order.Approve(_timeProvider);
        order.Ship(_timeProvider);

        var ex = Assert.Throws<DomainValidationException>(() => order.Cancel(_timeProvider));
        Assert.Contains("Shipped", ex.Message);
    }

    [Fact]
    public void Cancel_FromDelivered_ThrowsValidationException()
    {
        var order = CreateDraftOrderWithItem();
        order.Submit(_timeProvider);
        order.Approve(_timeProvider);
        order.Ship(_timeProvider);
        order.Deliver(_timeProvider);

        var ex = Assert.Throws<DomainValidationException>(() => order.Cancel(_timeProvider));
        Assert.Contains("Delivered", ex.Message);
    }

    [Fact]
    public void Submit_FromApproved_ThrowsValidationException()
    {
        var order = CreateDraftOrderWithItem();
        order.Submit(_timeProvider);
        order.Approve(_timeProvider);

        var ex = Assert.Throws<DomainValidationException>(() => order.Submit(_timeProvider));
        Assert.Contains("Draft", ex.Message);
    }

    [Fact]
    public void Approve_FromDraft_ThrowsValidationException()
    {
        var order = CreateDraftOrderWithItem();

        var ex = Assert.Throws<DomainValidationException>(() => order.Approve(_timeProvider));
        Assert.Contains("Submitted", ex.Message);
    }

    [Fact]
    public void Ship_FromDraft_ThrowsValidationException()
    {
        var order = CreateDraftOrderWithItem();

        var ex = Assert.Throws<DomainValidationException>(() => order.Ship(_timeProvider));
        Assert.Contains("Approved", ex.Message);
    }

    [Fact]
    public void Deliver_FromApproved_ThrowsValidationException()
    {
        var order = CreateDraftOrderWithItem();
        order.Submit(_timeProvider);
        order.Approve(_timeProvider);

        var ex = Assert.Throws<DomainValidationException>(() => order.Deliver(_timeProvider));
        Assert.Contains("Shipped", ex.Message);
    }

    [Fact]
    public void RequiresStockRelease_WhenSubmitted_IsTrue()
    {
        var order = CreateDraftOrderWithItem();
        order.Submit(_timeProvider);

        Assert.True(order.RequiresStockRelease);
    }

    [Fact]
    public void RequiresStockRelease_WhenApproved_IsTrue()
    {
        var order = CreateDraftOrderWithItem();
        order.Submit(_timeProvider);
        order.Approve(_timeProvider);

        Assert.True(order.RequiresStockRelease);
    }

    [Fact]
    public void RequiresStockRelease_WhenDraft_IsFalse()
    {
        var order = CreateDraftOrderWithItem();

        Assert.False(order.RequiresStockRelease);
    }

    [Fact]
    public void StockReservation_OnSubmit_DecreasesStock()
    {
        var product = Product.Create("Widget", "WGT001", 10.00m);
        product.AddStock(100);
        var customerId = Guid.NewGuid();
        var order = Order.Create(customerId, "actor-1", _baseTime);
        var lineItem = LineItem.Create(order.Id, product.Id, product.ProductName, 5, product.UnitPrice);
        order.AddLineItem(lineItem);

        // Service layer reserves stock after calling Submit
        product.ReserveStock(lineItem.Quantity);
        order.Submit(_timeProvider);

        Assert.Equal(95, product.StockQuantity);
        Assert.Equal(OrderStatus.Submitted, order.Status);
    }

    [Fact]
    public void StockRelease_OnCancelFromSubmitted_RestoresStock()
    {
        var product = Product.Create("Widget", "WGT001", 10.00m);
        product.AddStock(100);
        var customerId = Guid.NewGuid();
        var order = Order.Create(customerId, "actor-1", _baseTime);
        var lineItem = LineItem.Create(order.Id, product.Id, product.ProductName, 5, product.UnitPrice);
        order.AddLineItem(lineItem);

        product.ReserveStock(5);
        order.Submit(_timeProvider);

        // Simulate cancel with stock release
        var shouldRelease = order.RequiresStockRelease;
        order.Cancel(_timeProvider);
        if (shouldRelease)
            product.ReleaseStock(5);

        Assert.Equal(100, product.StockQuantity);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }
}
