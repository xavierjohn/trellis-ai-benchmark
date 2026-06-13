using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Products;
using static OrderManagement.Tests.Domain.DomainFixtures;

namespace OrderManagement.Tests.Domain;

public class OverdueSpecificationTests
{
    private static readonly DateTimeOffset Submitted = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private readonly Guid _customer = Guid.NewGuid();

    private Order SubmittedOrder()
    {
        var p = ProductWithStock("SKU001", 10m, 100);
        var order = DraftOrder(_customer, "actor-1", (p, 1));
        order.Submit(Dict(p), Submitted);
        return order;
    }

    [Fact]
    public void IsOverdue_WhenSubmitted8DaysAgo_True()
    {
        var order = SubmittedOrder();
        var now = Submitted.AddDays(8);

        Assert.True(OverdueOrderSpecification.IsOverdue(order, now));
    }

    [Fact]
    public void IsOverdue_WhenSubmittedRecently_False()
    {
        var order = SubmittedOrder();
        var now = Submitted.AddDays(3);

        Assert.False(OverdueOrderSpecification.IsOverdue(order, now));
    }

    [Fact]
    public void IsOverdue_WhenExactlySevenDays_False()
    {
        var order = SubmittedOrder();
        var now = Submitted.AddDays(7);

        Assert.False(OverdueOrderSpecification.IsOverdue(order, now));
    }

    [Fact]
    public void IsOverdue_WhenApproved_False()
    {
        var order = SubmittedOrder();
        order.Approve(Submitted.AddDays(1));
        var now = Submitted.AddDays(30);

        Assert.False(OverdueOrderSpecification.IsOverdue(order, now));
    }

    [Fact]
    public void IsOverdue_WhenDraft_False()
    {
        var p = ProductWithStock("SKU001", 10m, 100);
        var order = DraftOrder(_customer, "actor-1", (p, 1));

        Assert.False(OverdueOrderSpecification.IsOverdue(order, Submitted.AddDays(30)));
    }
}
