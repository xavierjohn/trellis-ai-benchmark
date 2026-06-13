namespace OrderManagement.Domain.Tests;

public class OverdueOrderSpecificationTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 30, 0, 0, 0, TimeSpan.Zero);

    private static Order SubmittedDaysAgo(int days)
    {
        var order = TestData.DraftOrder();
        order.Submit(new FixedTimeProvider(Now.AddDays(-days))).Discard();
        return order;
    }

    [Fact]
    public void Matches_OrderSubmitted8DaysAgo()
    {
        var order = SubmittedDaysAgo(8);
        var spec = new OverdueOrderSpecification(Now.UtcDateTime);
        spec.IsSatisfiedBy(order).Should().BeTrue();
    }

    [Fact]
    public void Excludes_RecentlySubmittedOrder()
    {
        var order = SubmittedDaysAgo(3);
        var spec = new OverdueOrderSpecification(Now.UtcDateTime);
        spec.IsSatisfiedBy(order).Should().BeFalse();
    }

    [Fact]
    public void Excludes_ApprovedOrder()
    {
        var order = SubmittedDaysAgo(10);
        order.Approve(new FixedTimeProvider(Now)).Discard();
        var spec = new OverdueOrderSpecification(Now.UtcDateTime);
        spec.IsSatisfiedBy(order).Should().BeFalse();
    }

    [Fact]
    public void Excludes_DraftOrder()
    {
        var order = TestData.DraftOrder();
        var spec = new OverdueOrderSpecification(Now.UtcDateTime);
        spec.IsSatisfiedBy(order).Should().BeFalse();
    }
}
