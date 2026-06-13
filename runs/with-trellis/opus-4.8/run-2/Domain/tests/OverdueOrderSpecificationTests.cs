namespace Domain.Tests;

using OrderManagement.Domain;

public class OverdueOrderSpecificationTests
{
    private static Order SubmittedAt(DateTimeOffset submittedAt)
    {
        var order = Build.DraftOrder();
        order.Submit(new FixedTimeProvider(submittedAt)).Unwrap();
        return order;
    }

    [Fact]
    public void Order_submitted_8_days_ago_is_overdue()
    {
        var asOf = new DateTimeOffset(2026, 1, 20, 0, 0, 0, TimeSpan.Zero);
        var order = SubmittedAt(asOf.AddDays(-8));

        var spec = new OverdueOrderSpecification(asOf.UtcDateTime);

        spec.IsSatisfiedBy(order).Should().BeTrue();
    }

    [Fact]
    public void Order_submitted_6_days_ago_is_not_overdue()
    {
        var asOf = new DateTimeOffset(2026, 1, 20, 0, 0, 0, TimeSpan.Zero);
        var order = SubmittedAt(asOf.AddDays(-6));

        var spec = new OverdueOrderSpecification(asOf.UtcDateTime);

        spec.IsSatisfiedBy(order).Should().BeFalse();
    }

    [Fact]
    public void Approved_order_is_not_overdue()
    {
        var asOf = new DateTimeOffset(2026, 1, 20, 0, 0, 0, TimeSpan.Zero);
        var order = SubmittedAt(asOf.AddDays(-10));
        order.Approve(new FixedTimeProvider(asOf.AddDays(-9))).Unwrap();

        var spec = new OverdueOrderSpecification(asOf.UtcDateTime);

        spec.IsSatisfiedBy(order).Should().BeFalse();
    }

    [Fact]
    public void Draft_order_is_not_overdue()
    {
        var order = Build.DraftOrder();
        var spec = new OverdueOrderSpecification(new DateTime(2026, 1, 20));

        spec.IsSatisfiedBy(order).Should().BeFalse();
    }
}
