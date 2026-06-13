using OrderManagement.Domain.Aggregates;
using OrderManagement.Domain.Enums;
using OrderManagement.Domain.Exceptions;
using Xunit;

namespace OrderManagement.Tests.Domain;

public class StateMachineTests
{
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private static Order CreateSubmittedOrder(out int reservedQty)
    {
        var order = Order.Create(CustomerId, "actor-1", Now);
        order.AddLineItem(ProductId, "Widget", 3, 10m);
        var qty = 0;
        order.Submit(Now, (_, q) => qty = q);
        reservedQty = qty;
        return order;
    }

    [Fact]
    public void Submit_FromDraft_Succeeds()
    {
        var order = Order.Create(CustomerId, "actor-1", Now);
        order.AddLineItem(ProductId, "Widget", 2, 10m);

        var reservations = new List<(Guid, int)>();
        var evt = order.Submit(Now, (pid, qty) => reservations.Add((pid, qty)));

        Assert.Equal(OrderStatus.Submitted, order.Status);
        Assert.NotNull(order.SubmittedAt);
        Assert.Single(reservations);
        Assert.Equal((ProductId, 2), reservations[0]);
        Assert.Equal(Now, evt.SubmittedAt);
        Assert.Equal(20m, evt.OrderTotal);
    }

    [Fact]
    public void Submit_FromDraft_ReservesStock()
    {
        var order = Order.Create(CustomerId, "actor-1", Now);
        order.AddLineItem(ProductId, "Widget", 5, 10m);

        int reserved = 0;
        order.Submit(Now, (_, qty) => reserved = qty);

        Assert.Equal(5, reserved);
    }

    [Fact]
    public void Submit_FromSubmitted_ThrowsValidation()
    {
        var order = CreateSubmittedOrder(out _);
        var ex = Assert.Throws<ValidationException>(() =>
            order.Submit(Now, (_, _) => { }));
        Assert.Contains("Draft", ex.Message);
    }

    [Fact]
    public void Submit_WithNoLineItems_ThrowsValidation()
    {
        var order = Order.Create(CustomerId, "actor-1", Now);
        Assert.Throws<ValidationException>(() => order.Submit(Now, (_, _) => { }));
    }

    [Fact]
    public void Approve_FromSubmitted_Succeeds()
    {
        var order = CreateSubmittedOrder(out _);
        var evt = order.Approve(Now);

        Assert.Equal(OrderStatus.Approved, order.Status);
        Assert.Equal(order.Id, evt.OrderId);
    }

    [Fact]
    public void Approve_FromDraft_ThrowsValidation()
    {
        var order = Order.Create(CustomerId, "actor-1", Now);
        order.AddLineItem(ProductId, "Widget", 1, 10m);
        Assert.Throws<ValidationException>(() => order.Approve(Now));
    }

    [Fact]
    public void Ship_FromApproved_Succeeds()
    {
        var order = CreateSubmittedOrder(out _);
        order.Approve(Now);
        var evt = order.Ship(Now);

        Assert.Equal(OrderStatus.Shipped, order.Status);
        Assert.NotNull(order.ShippedAt);
    }

    [Fact]
    public void Ship_FromSubmitted_ThrowsValidation()
    {
        var order = CreateSubmittedOrder(out _);
        Assert.Throws<ValidationException>(() => order.Ship(Now));
    }

    [Fact]
    public void Deliver_FromShipped_Succeeds()
    {
        var order = CreateSubmittedOrder(out _);
        order.Approve(Now);
        order.Ship(Now);
        var evt = order.Deliver(Now);

        Assert.Equal(OrderStatus.Delivered, order.Status);
    }

    [Fact]
    public void Deliver_FromApproved_ThrowsValidation()
    {
        var order = CreateSubmittedOrder(out _);
        order.Approve(Now);
        Assert.Throws<ValidationException>(() => order.Deliver(Now));
    }

    [Fact]
    public void Cancel_FromDraft_Succeeds_NoStockRelease()
    {
        var order = Order.Create(CustomerId, "actor-1", Now);
        order.AddLineItem(ProductId, "Widget", 2, 10m);

        var releases = new List<(Guid, int)>();
        var evt = order.Cancel(Now, (pid, qty) => releases.Add((pid, qty)));

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Empty(releases);
        Assert.Equal("Draft", evt.CancelledFromStatus);
    }

    [Fact]
    public void Cancel_FromSubmitted_ReleasesStock()
    {
        var order = CreateSubmittedOrder(out _);

        var releases = new List<(Guid, int)>();
        var evt = order.Cancel(Now, (pid, qty) => releases.Add((pid, qty)));

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Single(releases);
        Assert.Equal((ProductId, 3), releases[0]);
        Assert.Equal("Submitted", evt.CancelledFromStatus);
    }

    [Fact]
    public void Cancel_FromApproved_ReleasesStock()
    {
        var order = CreateSubmittedOrder(out _);
        order.Approve(Now);

        var releases = new List<(Guid, int)>();
        order.Cancel(Now, (pid, qty) => releases.Add((pid, qty)));

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Single(releases);
    }

    [Fact]
    public void Cancel_FromShipped_ThrowsValidation()
    {
        var order = CreateSubmittedOrder(out _);
        order.Approve(Now);
        order.Ship(Now);

        Assert.Throws<ValidationException>(() => order.Cancel(Now, (_, _) => { }));
    }

    [Fact]
    public void Cancel_FromDelivered_ThrowsValidation()
    {
        var order = CreateSubmittedOrder(out _);
        order.Approve(Now);
        order.Ship(Now);
        order.Deliver(Now);

        Assert.Throws<ValidationException>(() => order.Cancel(Now, (_, _) => { }));
    }

    [Fact]
    public void InvalidTransition_Draft_ToApproved_ThrowsValidation()
    {
        var order = Order.Create(CustomerId, "actor-1", Now);
        order.AddLineItem(ProductId, "Widget", 1, 10m);
        // Cannot go Draft -> Approved directly
        Assert.Throws<ValidationException>(() => order.Approve(Now));
    }

    [Fact]
    public void Overdue_SubmittedMoreThan7DaysAgo_IsOverdue()
    {
        var submittedAt = Now.AddDays(-8);
        var order = Order.Create(CustomerId, "actor-1", submittedAt.AddHours(-1));
        order.AddLineItem(ProductId, "Widget", 1, 10m);
        order.Submit(submittedAt, (_, _) => { });

        Assert.True(order.IsOverdue(Now));
    }

    [Fact]
    public void Overdue_SubmittedLessThan7DaysAgo_IsNotOverdue()
    {
        var submittedAt = Now.AddDays(-6);
        var order = Order.Create(CustomerId, "actor-1", submittedAt.AddHours(-1));
        order.AddLineItem(ProductId, "Widget", 1, 10m);
        order.Submit(submittedAt, (_, _) => { });

        Assert.False(order.IsOverdue(Now));
    }

    [Fact]
    public void Overdue_ApprovedOrder_IsNotOverdue()
    {
        var submittedAt = Now.AddDays(-10);
        var order = Order.Create(CustomerId, "actor-1", submittedAt.AddHours(-1));
        order.AddLineItem(ProductId, "Widget", 1, 10m);
        order.Submit(submittedAt, (_, _) => { });
        order.Approve(Now.AddDays(-9));

        Assert.False(order.IsOverdue(Now));
    }
}
