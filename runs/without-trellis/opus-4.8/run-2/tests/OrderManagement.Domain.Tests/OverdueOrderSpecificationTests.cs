using OrderManagement.Domain.Orders;
using Xunit;

namespace OrderManagement.Domain.Tests;

public class OverdueOrderSpecificationTests
{
    [Fact]
    public void Order_submitted_8_days_ago_is_overdue()
    {
        var product = TestData.ProductWithStock(stock: 10);
        var submittedAt = TestData.Now;
        var order = TestData.DraftOrder(product, qty: 1);
        order.Submit(new[] { product }, submittedAt);

        var now = submittedAt.AddDays(8);

        Assert.True(OverdueOrderSpecification.IsOverdue(order, now));
    }

    [Fact]
    public void Order_submitted_recently_is_not_overdue()
    {
        var product = TestData.ProductWithStock(stock: 10);
        var submittedAt = TestData.Now;
        var order = TestData.DraftOrder(product, qty: 1);
        order.Submit(new[] { product }, submittedAt);

        var now = submittedAt.AddDays(3);

        Assert.False(OverdueOrderSpecification.IsOverdue(order, now));
    }

    [Fact]
    public void Order_exactly_7_days_is_not_overdue()
    {
        var product = TestData.ProductWithStock(stock: 10);
        var submittedAt = TestData.Now;
        var order = TestData.DraftOrder(product, qty: 1);
        order.Submit(new[] { product }, submittedAt);

        var now = submittedAt.AddDays(7);

        Assert.False(OverdueOrderSpecification.IsOverdue(order, now));
    }

    [Fact]
    public void Approved_order_is_not_overdue()
    {
        var product = TestData.ProductWithStock(stock: 10);
        var submittedAt = TestData.Now;
        var order = TestData.DraftOrder(product, qty: 1);
        order.Submit(new[] { product }, submittedAt);
        order.Approve(submittedAt);

        var now = submittedAt.AddDays(30);

        Assert.False(OverdueOrderSpecification.IsOverdue(order, now));
    }

    [Fact]
    public void Draft_order_is_not_overdue()
    {
        var product = TestData.ProductWithStock(stock: 10);
        var order = TestData.DraftOrder(product, qty: 1);

        Assert.False(OverdueOrderSpecification.IsOverdue(order, TestData.Now.AddDays(30)));
    }
}
