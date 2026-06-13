namespace Domain.Tests;

using System.Globalization;
using Microsoft.Extensions.Time.Testing;
using OrderManagement.Domain;
using Trellis.Authorization;

public class OverdueOrderSpecificationTests
{
    [Fact]
    public void ToExpression_matches_submitted_orders_older_than_seven_days()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-01-10T00:00:00Z", CultureInfo.InvariantCulture));
        var product = new Product(ProductName.Create("Widget"), Sku.Create("SKU123"), 25m);
        product.AddStock(10).Should().BeSuccess();
        var order = new Order(CustomerId.NewUniqueV7(), ActorId.Create("owner-1"),
        [
            new LineItem(product.Id, product.Name.Value, 1, product.UnitPrice),
        ]);
        order.Submit([product], timeProvider).Should().BeSuccess();

        var specification = new OverdueOrderSpecification(timeProvider.GetUtcNow().UtcDateTime.AddDays(8));
        var predicate = specification.ToExpression().Compile();

        predicate(order).Should().BeTrue();
    }
}
