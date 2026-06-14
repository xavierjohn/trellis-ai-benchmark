namespace Domain.Tests;

using OrderManagement.Domain;

public class OverdueOrderSpecificationTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 15, 0, 0, 0, TimeSpan.Zero);

    private static Order SubmittedDaysAgo(int days)
    {
        var product = TestData.Product(stock: 100);
        var order = TestData.DraftOrder(product);
        var products = new Dictionary<ProductId, Product> { [product.Id] = product };
        var submitTime = new FixedTimeProvider(Now.AddDays(-days));
        order.Submit(products, submitTime).Unwrap();
        return order;
    }

    [Fact]
    public void Matches_order_submitted_8_days_ago()
    {
        var order = SubmittedDaysAgo(8);
        var spec = new OverdueOrderSpecification(new FixedTimeProvider(Now));

        spec.IsSatisfiedBy(order).Should().BeTrue();
    }

    [Fact]
    public void Excludes_recent_submitted_order()
    {
        var order = SubmittedDaysAgo(2);
        var spec = new OverdueOrderSpecification(new FixedTimeProvider(Now));

        spec.IsSatisfiedBy(order).Should().BeFalse();
    }

    [Fact]
    public void Excludes_approved_order()
    {
        var product = TestData.Product(stock: 100);
        var order = TestData.DraftOrder(product);
        var products = new Dictionary<ProductId, Product> { [product.Id] = product };
        var old = new FixedTimeProvider(Now.AddDays(-20));
        order.Submit(products, old).Unwrap();
        order.Approve(old).Unwrap();

        var spec = new OverdueOrderSpecification(new FixedTimeProvider(Now));

        spec.IsSatisfiedBy(order).Should().BeFalse();
    }

    [Fact]
    public void Excludes_draft_order()
    {
        var order = TestData.DraftOrder(TestData.Product());
        var spec = new OverdueOrderSpecification(new FixedTimeProvider(Now));

        spec.IsSatisfiedBy(order).Should().BeFalse();
    }
}
