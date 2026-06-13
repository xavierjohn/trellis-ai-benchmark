namespace Domain.Tests;

using OrderManagement.Domain;

public class OverdueOrderSpecificationTests
{
    private static CustomerId TestCustomerId => CustomerId.NewUniqueV7();

    private static Order CreateSubmittedOrderAt(DateTime submittedAt)
    {
        var order = new Order(TestCustomerId, "actor-1");
        order.AddLineItem(
            ProductId.NewUniqueV7(),
            ProductName.Create("Test Product"),
            Quantity.Create(1),
            UnitPrice.Create(10.00m)).Should().BeSuccess();

        var fakeTimeProvider = new FixedTimeProvider(submittedAt);
        order.Submit(fakeTimeProvider).Should().BeSuccess();
        return order;
    }

    [Fact]
    public void Matches_submitted_order_older_than_7_days()
    {
        var spec = new OverdueOrderSpecification(TimeProvider.System);
        var oldOrder = CreateSubmittedOrderAt(DateTime.UtcNow.AddDays(-8));

        spec.IsSatisfiedBy(oldOrder).Should().BeTrue();
    }

    [Fact]
    public void Does_not_match_submitted_order_within_7_days()
    {
        var spec = new OverdueOrderSpecification(TimeProvider.System);
        var recentOrder = CreateSubmittedOrderAt(DateTime.UtcNow.AddDays(-3));

        spec.IsSatisfiedBy(recentOrder).Should().BeFalse();
    }

    [Fact]
    public void Does_not_match_draft_order()
    {
        var spec = new OverdueOrderSpecification(TimeProvider.System);
        var order = new Order(TestCustomerId, "actor-1");
        order.AddLineItem(ProductId.NewUniqueV7(), ProductName.Create("P"), Quantity.Create(1), UnitPrice.Create(5.00m)).Should().BeSuccess();

        spec.IsSatisfiedBy(order).Should().BeFalse();
    }

    [Fact]
    public void Does_not_match_approved_order()
    {
        var spec = new OverdueOrderSpecification(TimeProvider.System);
        var order = new Order(TestCustomerId, "actor-1");
        order.AddLineItem(ProductId.NewUniqueV7(), ProductName.Create("P"), Quantity.Create(1), UnitPrice.Create(5.00m)).Should().BeSuccess();
        order.Submit(new FixedTimeProvider(DateTime.UtcNow.AddDays(-10))).Should().BeSuccess();
        order.Approve(TimeProvider.System).Should().BeSuccess();

        spec.IsSatisfiedBy(order).Should().BeFalse();
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;
        public FixedTimeProvider(DateTime utcNow) => _utcNow = new DateTimeOffset(utcNow, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => _utcNow;
    }
}
