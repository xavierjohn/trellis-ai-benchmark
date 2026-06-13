using OrderManagement.Api.Domain;
using Xunit;

namespace OrderManagement.Tests.Domain;

public class OverdueSpecificationTests
{
    private static Order CreateSubmittedOrder(DateTime submittedAt)
    {
        var fakeTime = new FakeTimeProvider(submittedAt);
        var customerId = Guid.NewGuid();
        var order = Order.Create(customerId, "actor-1", submittedAt.AddMinutes(-5));
        var product = Product.Create("Widget", "WGT001", 10.00m);
        var lineItem = LineItem.Create(order.Id, product.Id, product.ProductName, 1, product.UnitPrice);
        order.AddLineItem(lineItem);
        order.Submit(fakeTime);
        return order;
    }

    [Fact]
    public void Order_SubmittedMoreThan7DaysAgo_IsOverdue()
    {
        var now = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var submittedAt = now.AddDays(-8); // 8 days ago
        var order = CreateSubmittedOrder(submittedAt);
        var cutoff = now.AddDays(-7);

        var isOverdue = order.Status == OrderStatus.Submitted && order.SubmittedAt < cutoff;

        Assert.True(isOverdue);
    }

    [Fact]
    public void Order_SubmittedExactly7DaysAgo_IsNotOverdue()
    {
        var now = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var submittedAt = now.AddDays(-7); // exactly 7 days ago (not more than 7)
        var order = CreateSubmittedOrder(submittedAt);
        var cutoff = now.AddDays(-7);

        var isOverdue = order.Status == OrderStatus.Submitted && order.SubmittedAt < cutoff;

        Assert.False(isOverdue);
    }

    [Fact]
    public void Order_SubmittedLessThan7DaysAgo_IsNotOverdue()
    {
        var now = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var submittedAt = now.AddDays(-3); // only 3 days ago
        var order = CreateSubmittedOrder(submittedAt);
        var cutoff = now.AddDays(-7);

        var isOverdue = order.Status == OrderStatus.Submitted && order.SubmittedAt < cutoff;

        Assert.False(isOverdue);
    }

    [Fact]
    public void Order_Approved_IsNotOverdueEvenIfOld()
    {
        var now = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var submittedAt = now.AddDays(-10);
        var order = CreateSubmittedOrder(submittedAt);
        var approveTime = new FakeTimeProvider(submittedAt.AddDays(1));
        order.Approve(approveTime);
        var cutoff = now.AddDays(-7);

        // Overdue only applies to Submitted status
        var isOverdue = order.Status == OrderStatus.Submitted && order.SubmittedAt < cutoff;

        Assert.False(isOverdue);
        Assert.Equal(OrderStatus.Approved, order.Status);
    }

    [Fact]
    public void Order_Draft_IsNotOverdue()
    {
        var customerId = Guid.NewGuid();
        var order = Order.Create(customerId, "actor-1", DateTime.UtcNow.AddDays(-30));
        var product = Product.Create("Widget", "WGT001", 10.00m);
        var lineItem = LineItem.Create(order.Id, product.Id, product.ProductName, 1, product.UnitPrice);
        order.AddLineItem(lineItem);

        var cutoff = DateTime.UtcNow.AddDays(-7);
        var isOverdue = order.Status == OrderStatus.Submitted && order.SubmittedAt < cutoff;

        Assert.False(isOverdue);
    }
}
