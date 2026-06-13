using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Products;

namespace OrderManagement.Tests.Domain;

public class OverdueOrderSpecificationTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-06-12T00:00:00Z");

    private static Order SubmittedOrderAt(DateTimeOffset submittedAt)
    {
        var product = Product.Create("Widget", "WIDGET01", 9.99m, 100).Value;
        var requests = new List<(Order.LineRequest, Product)> { (new Order.LineRequest(product.Id, 1), product) };
        var order = Order.CreateDraft(Guid.NewGuid(), "actor-1", requests, submittedAt).Value;
        order.Submit(new Dictionary<Guid, Product> { [product.Id] = product }, submittedAt);
        return order;
    }

    [Fact]
    public void Order_SubmittedEightDaysAgo_IsOverdue()
    {
        var order = SubmittedOrderAt(Now.AddDays(-8));

        Assert.True(OverdueOrderSpecification.IsOverdue(order, Now));
    }

    [Fact]
    public void Order_SubmittedSixDaysAgo_IsNotOverdue()
    {
        var order = SubmittedOrderAt(Now.AddDays(-6));

        Assert.False(OverdueOrderSpecification.IsOverdue(order, Now));
    }

    [Fact]
    public void Order_SubmittedExactlySevenDaysAgo_IsNotOverdue()
    {
        var order = SubmittedOrderAt(Now.AddDays(-7));

        // "more than 7 days" → exactly 7 days is not overdue.
        Assert.False(OverdueOrderSpecification.IsOverdue(order, Now));
    }

    [Fact]
    public void ApprovedOrder_IsNeverOverdue()
    {
        var order = SubmittedOrderAt(Now.AddDays(-30));
        order.Approve(Now);

        Assert.False(OverdueOrderSpecification.IsOverdue(order, Now));
    }

    [Fact]
    public void DraftOrder_IsNotOverdue()
    {
        var product = Product.Create("Widget", "WIDGET01", 9.99m, 100).Value;
        var requests = new List<(Order.LineRequest, Product)> { (new Order.LineRequest(product.Id, 1), product) };
        var order = Order.CreateDraft(Guid.NewGuid(), "actor-1", requests, Now.AddDays(-30)).Value;

        Assert.False(OverdueOrderSpecification.IsOverdue(order, Now));
    }
}
