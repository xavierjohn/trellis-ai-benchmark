namespace Domain.Tests;

using OrderManagement.Domain;

public class OrderTests
{
    [Fact]
    public void CreateDraft_with_line_items_succeeds()
    {
        var product = TestData.Product();

        var order = TestData.DraftOrder(product, quantity: 3);

        order.Status.Should().Be(OrderStatus.Draft);
        order.LineItems.Should().HaveCount(1);
        order.OrderTotal.Should().Be(product.UnitPrice.Value * 3);
    }

    [Fact]
    public void CreateDraft_without_line_items_fails()
    {
        Order.CreateDraft(CustomerId.NewUniqueV7(), "a", [])
            .Should().BeFailureOfType<Error.InvalidInput>();
    }

    [Fact]
    public void CreateDraft_with_duplicate_products_fails()
    {
        var product = TestData.Product();
        var line = new DraftLineItem(product.Id, product.Name, Quantity.Create(1), product.UnitPrice);

        Order.CreateDraft(CustomerId.NewUniqueV7(), "a", [line, line])
            .Should().BeFailureOfType<Error.InvalidInput>();
    }

    [Fact]
    public void AddLineItem_with_new_product_succeeds()
    {
        var order = TestData.DraftOrder(TestData.Product());
        var other = TestData.Product(sku: "XYZ999");

        var result = order.AddLineItem(other.Id, other.Name, Quantity.Create(1), other.UnitPrice);

        result.Should().BeSuccess();
        order.LineItems.Should().HaveCount(2);
    }

    [Fact]
    public void AddLineItem_with_duplicate_product_fails()
    {
        var product = TestData.Product();
        var order = TestData.DraftOrder(product);

        order.AddLineItem(product.Id, product.Name, Quantity.Create(1), product.UnitPrice)
            .Should().BeFailureOfType<Error.InvalidInput>();
    }

    [Fact]
    public void RemoveLineItem_when_more_than_one_succeeds()
    {
        var product = TestData.Product();
        var order = TestData.DraftOrder(product);
        var other = TestData.Product(sku: "XYZ999");
        order.AddLineItem(other.Id, other.Name, Quantity.Create(1), other.UnitPrice).Unwrap();
        var toRemove = order.LineItems[0].Id;

        var result = order.RemoveLineItem(toRemove);

        result.Should().BeSuccess();
        order.LineItems.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveLineItem_last_item_fails()
    {
        var order = TestData.DraftOrder(TestData.Product());
        var only = order.LineItems[0].Id;

        order.RemoveLineItem(only).Should().BeFailureOfType<Error.InvalidInput>();
    }

    [Fact]
    public void RemoveLineItem_not_found_returns_not_found()
    {
        var order = TestData.DraftOrder(TestData.Product());

        order.RemoveLineItem(LineItemId.NewUniqueV7()).Should().BeFailureOfType<Error.NotFound>();
    }
}
