using Domain.Orders;
using Domain.Products;

namespace Domain.Tests;

public class OverdueOrderTests
{
    [Fact]
    public void SubmittedMoreThan7DaysAgo_IsOverdue()
    {
        var fakeTime = new FakeTimeProvider(DateTime.UtcNow);
        var order = Order.Create(Guid.NewGuid(), "actor-1", fakeTime).Value!;
        var product = Product.Create("Widget", "WDG001", 9.99m, 100).Value!;
        order.AddLineItem(product.ProductId, product.ProductName, 1, product.UnitPrice);
        order.Submit([product], fakeTime);
        fakeTime.Advance(TimeSpan.FromDays(8));
        Assert.True(OverdueOrderSpecification.IsSatisfiedBy(order, fakeTime));
    }

    [Fact]
    public void SubmittedLessThan7DaysAgo_IsNotOverdue()
    {
        var fakeTime = new FakeTimeProvider(DateTime.UtcNow);
        var order = Order.Create(Guid.NewGuid(), "actor-1", fakeTime).Value!;
        var product = Product.Create("Widget", "WDG001", 9.99m, 100).Value!;
        order.AddLineItem(product.ProductId, product.ProductName, 1, product.UnitPrice);
        order.Submit([product], fakeTime);
        fakeTime.Advance(TimeSpan.FromDays(3));
        Assert.False(OverdueOrderSpecification.IsSatisfiedBy(order, fakeTime));
    }

    [Fact]
    public void ApprovedOrder_IsNotOverdue()
    {
        var fakeTime = new FakeTimeProvider(DateTime.UtcNow);
        var order = Order.Create(Guid.NewGuid(), "actor-1", fakeTime).Value!;
        var product = Product.Create("Widget", "WDG001", 9.99m, 100).Value!;
        order.AddLineItem(product.ProductId, product.ProductName, 1, product.UnitPrice);
        order.Submit([product], fakeTime);
        order.Approve();
        fakeTime.Advance(TimeSpan.FromDays(8));
        Assert.False(OverdueOrderSpecification.IsSatisfiedBy(order, fakeTime));
    }
}

public class FakeTimeProvider : TimeProvider
{
    private DateTimeOffset _now;

    public FakeTimeProvider(DateTime utcNow)
    {
        _now = new DateTimeOffset(utcNow, TimeSpan.Zero);
    }

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan timeSpan)
    {
        _now = _now.Add(timeSpan);
    }
}
