namespace Domain.Tests;

using Microsoft.Extensions.Time.Testing;
using OrderManagement.Domain;
using Trellis.Testing;

public class OverdueOrderSpecificationTests
{
    private static Order MakeSubmittedOrder(DateTime submittedAt)
    {
        var customerId = CustomerId.Create(Guid.NewGuid());
        var item = new LineItem(ProductId.Create(Guid.NewGuid()), ProductName.TryCreate("Widget").Unwrap(), 1, UnitPrice.TryCreate(5m).Unwrap());
        var order = new Order(customerId, "actor-1", [item]);
        var fakeTimeProvider = new FakeTimeProvider(new DateTimeOffset(submittedAt, TimeSpan.Zero));
        order.Submit(fakeTimeProvider).Should().BeSuccess();
        return order;
    }

    [Fact]
    public void Order_submitted_8_days_ago_is_overdue()
    {
        var now = DateTime.UtcNow;
        var order = MakeSubmittedOrder(now.AddDays(-8));
        var spec = new OverdueOrderSpecification(now.AddDays(-7));
        spec.IsSatisfiedBy(order).Should().BeTrue();
    }

    [Fact]
    public void Order_submitted_6_days_ago_is_not_overdue()
    {
        var now = DateTime.UtcNow;
        var order = MakeSubmittedOrder(now.AddDays(-6));
        var spec = new OverdueOrderSpecification(now.AddDays(-7));
        spec.IsSatisfiedBy(order).Should().BeFalse();
    }

    [Fact]
    public void Approved_order_is_not_overdue()
    {
        var now = DateTime.UtcNow;
        var order = MakeSubmittedOrder(now.AddDays(-8));
        order.Approve(TimeProvider.System).Should().BeSuccess();
        var spec = new OverdueOrderSpecification(now.AddDays(-7));
        spec.IsSatisfiedBy(order).Should().BeFalse();
    }

    [Fact]
    public void Draft_order_is_not_overdue()
    {
        var customerId = CustomerId.Create(Guid.NewGuid());
        var item = new LineItem(ProductId.Create(Guid.NewGuid()), ProductName.TryCreate("Widget").Unwrap(), 1, UnitPrice.TryCreate(5m).Unwrap());
        var order = new Order(customerId, "actor-1", [item]);
        var spec = new OverdueOrderSpecification(DateTime.UtcNow.AddDays(-7));
        spec.IsSatisfiedBy(order).Should().BeFalse();
    }
}
