namespace Domain.Tests;

using OrderManagement.Domain;
using Trellis.Primitives;

public class OrderTests
{
    [Fact]
    public void Create_with_line_items_succeeds_in_draft()
    {
        var order = Build.DraftOrder();

        order.Status.Should().Be(OrderStatus.Draft);
        order.LineItems.Should().HaveCount(1);
    }

    [Fact]
    public void Create_with_no_line_items_fails()
    {
        var result = Order.TryCreate(CustomerId.NewUniqueV7(), "actor-1", []);

        result.Should().BeFailureOfType<Error.InvalidInput>();
    }

    [Fact]
    public void Create_with_duplicate_products_fails()
    {
        var product = Build.Product(stock: 10);
        var result = Order.TryCreate(
            CustomerId.NewUniqueV7(),
            "actor-1",
            [Build.LineItem(product), Build.LineItem(product)]);

        result.Should().BeFailureOfType<Error.InvalidInput>();
    }

    [Fact]
    public void Total_is_sum_of_line_items()
    {
        var p1 = Build.Product(sku: "AAA111", price: 10m, stock: 10);
        var p2 = Build.Product(sku: "BBB222", price: 2.50m, stock: 10);
        var order = Order.TryCreate(
            CustomerId.NewUniqueV7(),
            "actor-1",
            [Build.LineItem(p1, 2), Build.LineItem(p2, 4)]).Unwrap();

        order.Total.Value.Should().Be(30m); // 10*2 + 2.5*4
    }

    [Fact]
    public void AddLineItem_to_draft_succeeds()
    {
        var order = Build.DraftOrder();
        var newProduct = Build.Product(sku: "OTHER001", stock: 5);

        var result = order.AddLineItem(Build.LineItem(newProduct));

        result.Should().BeSuccess();
        order.LineItems.Should().HaveCount(2);
    }

    [Fact]
    public void AddLineItem_with_duplicate_product_fails()
    {
        var product = Build.Product(stock: 10);
        var order = Build.DraftOrder(lineItems: Build.LineItem(product));

        var result = order.AddLineItem(Build.LineItem(product));

        result.Should().BeFailureOfType<Error.InvalidInput>();
    }

    [Fact]
    public void RemoveLineItem_succeeds_when_more_than_one()
    {
        var p1 = Build.Product(sku: "AAA111", stock: 10);
        var p2 = Build.Product(sku: "BBB222", stock: 10);
        var li1 = Build.LineItem(p1);
        var order = Build.DraftOrder(lineItems: new[] { li1, Build.LineItem(p2) });

        var result = order.RemoveLineItem(li1.Id);

        result.Should().BeSuccess();
        order.LineItems.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveLineItem_last_item_fails()
    {
        var li = Build.LineItem(Build.Product(stock: 10));
        var order = Build.DraftOrder(lineItems: li);

        var result = order.RemoveLineItem(li.Id);

        result.Should().BeFailureOfType<Error.InvalidInput>();
    }

    [Fact]
    public void RemoveLineItem_unknown_id_returns_not_found()
    {
        var order = Build.DraftOrder();

        var result = order.RemoveLineItem(LineItemId.NewUniqueV7());

        result.Should().BeFailureOfType<Error.NotFound>();
    }

    [Fact]
    public void Quantity_above_max_fails_validation()
    {
        Quantity.TryCreate(1000, "quantity").Should().BeFailure();
        Quantity.TryCreate(0, "quantity").Should().BeFailure();
        Quantity.TryCreate(999, "quantity").Should().BeSuccess();
    }
}
