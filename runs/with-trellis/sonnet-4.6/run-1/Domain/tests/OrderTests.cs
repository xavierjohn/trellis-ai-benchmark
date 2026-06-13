namespace Domain.Tests;

using System.Globalization;
using Microsoft.Extensions.Time.Testing;
using OrderManagement.Domain;
using Trellis.Authorization;

public class OrderTests
{
    [Fact]
    public void Submit_reserves_stock_and_sets_submitted_at()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-01-10T00:00:00Z", CultureInfo.InvariantCulture));
        var customerId = CustomerId.NewUniqueV7();
        var product = new Product(ProductName.Create("Widget"), Sku.Create("SKU123"), 25m);
        product.AddStock(10).Should().BeSuccess();
        var order = new Order(customerId, ActorId.Create("owner-1"),
        [
            new LineItem(product.Id, product.Name.Value, 2, product.UnitPrice),
        ]);

        var result = order.Submit([product], timeProvider);

        result.Should().BeSuccess();
        order.Status.Should().Be(OrderStatus.Submitted);
        order.SubmittedAt.Should().HaveValue();
        product.StockQuantity.Should().Be(8);
    }

    [Fact]
    public void Cancel_after_submission_releases_reserved_stock()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-01-10T00:00:00Z", CultureInfo.InvariantCulture));
        var customerId = CustomerId.NewUniqueV7();
        var product = new Product(ProductName.Create("Widget"), Sku.Create("SKU123"), 25m);
        product.AddStock(10).Should().BeSuccess();
        var order = new Order(customerId, ActorId.Create("owner-1"),
        [
            new LineItem(product.Id, product.Name.Value, 3, product.UnitPrice),
        ]);
        order.Submit([product], timeProvider).Should().BeSuccess();

        var cancelResult = order.Cancel([product], timeProvider);

        cancelResult.Should().BeSuccess();
        order.Status.Should().Be(OrderStatus.Cancelled);
        product.StockQuantity.Should().Be(10);
    }
}
