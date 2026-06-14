using Microsoft.Extensions.Time.Testing;
using OrderManagement.Api.Domain.Orders;
using OrderManagement.Api.Domain.Products;
using Xunit;

namespace OrderManagement.Tests.Domain;

public class OverdueSpecificationTests
{
    private static Order BuildSubmittedOrder(DateTimeOffset submittedAt)
    {
        var time = new FakeTimeProvider(submittedAt);
        var product = Product.Create("Widget", "WIDGET01", 10m, 100);
        var drafts = new List<LineItemDraft> { new(product.Id, "Widget", 10m, 1) };
        var order = Order.CreateDraft(Guid.NewGuid(), "actor-1", drafts, time);
        order.Submit(new Dictionary<Guid, Product> { [product.Id] = product }, time);
        return order;
    }

    [Fact]
    public void Order_Submitted8DaysAgo_IsOverdue()
    {
        var now = new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc);
        var order = BuildSubmittedOrder(now.AddDays(-8));

        Assert.True(OverdueOrderSpecification.IsOverdue(order, now));
    }

    [Fact]
    public void Order_SubmittedRecently_IsNotOverdue()
    {
        var now = new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc);
        var order = BuildSubmittedOrder(now.AddDays(-2));

        Assert.False(OverdueOrderSpecification.IsOverdue(order, now));
    }

    [Fact]
    public void Order_Submitted6DaysAgo_IsNotOverdue()
    {
        var now = new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc);
        var order = BuildSubmittedOrder(now.AddDays(-6));

        Assert.False(OverdueOrderSpecification.IsOverdue(order, now));
    }

    [Fact]
    public void ApprovedOrder_IsNeverOverdue()
    {
        var submittedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var time = new FakeTimeProvider(submittedAt);
        var product = Product.Create("Widget", "WIDGET01", 10m, 100);
        var drafts = new List<LineItemDraft> { new(product.Id, "Widget", 10m, 1) };
        var order = Order.CreateDraft(Guid.NewGuid(), "actor-1", drafts, time);
        order.Submit(new Dictionary<Guid, Product> { [product.Id] = product }, time);
        order.Approve(time);

        var now = submittedAt.UtcDateTime.AddDays(30);
        Assert.False(OverdueOrderSpecification.IsOverdue(order, now));
    }
}
