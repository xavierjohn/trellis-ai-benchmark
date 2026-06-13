namespace OrderManagement.Domain.Tests;

public class OrderTests
{
    private static Order DraftWithProducts(params (string name, int qty)[] items)
    {
        var lines = items
            .Select(i => new OrderLineInput(
                ProductId.NewUniqueV7(), TestData.ProductName(i.name), TestData.Quantity(i.qty), TestData.UnitPrice()))
            .ToList();
        return Order.Create(CustomerId.NewUniqueV7(), "actor-1", lines).Unwrap();
    }

    [Fact]
    public void Create_WithoutLineItems_Fails()
    {
        var result = Order.Create(CustomerId.NewUniqueV7(), "actor-1", []);
        result.Should().BeFailureOfType<Error.InvalidInput>();
    }

    [Fact]
    public void Create_WithDuplicateProducts_Fails()
    {
        var productId = ProductId.NewUniqueV7();
        var lines = new[]
        {
            new OrderLineInput(productId, TestData.ProductName(), TestData.Quantity(1), TestData.UnitPrice()),
            new OrderLineInput(productId, TestData.ProductName(), TestData.Quantity(2), TestData.UnitPrice()),
        };

        Order.Create(CustomerId.NewUniqueV7(), "actor-1", lines)
            .Should().BeFailureOfType<Error.InvalidInput>();
    }

    [Fact]
    public void Create_CapturesUnitPrices_AndComputesTotal()
    {
        var order = Order.Create(
            CustomerId.NewUniqueV7(), "actor-1",
            [new OrderLineInput(ProductId.NewUniqueV7(), TestData.ProductName(), TestData.Quantity(3), TestData.UnitPrice(10m))]).Unwrap();

        order.OrderTotal.Should().Be(30m);
        order.Status.Should().Be(OrderStatus.Draft);
    }

    [Fact]
    public void AddLineItem_NewProduct_Succeeds()
    {
        var order = DraftWithProducts(("A", 1));
        order.AddLineItem(ProductId.NewUniqueV7(), TestData.ProductName("B"), TestData.Quantity(2), TestData.UnitPrice())
            .Should().BeSuccess();
        order.LineItems.Should().HaveCount(2);
    }

    [Fact]
    public void AddLineItem_DuplicateProduct_Fails()
    {
        var order = DraftWithProducts(("A", 1));
        var existing = order.LineItems[0].ProductId;
        order.AddLineItem(existing, TestData.ProductName("A"), TestData.Quantity(1), TestData.UnitPrice())
            .Should().BeFailureOfType<Error.InvalidInput>();
    }

    [Fact]
    public void RemoveLineItem_WhenMoreThanOne_Succeeds()
    {
        var order = DraftWithProducts(("A", 1), ("B", 1));
        var toRemove = order.LineItems[0].Id;
        order.RemoveLineItem(toRemove).Should().BeSuccess();
        order.LineItems.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveLineItem_LastOne_Fails()
    {
        var order = DraftWithProducts(("A", 1));
        order.RemoveLineItem(order.LineItems[0].Id).Should().BeFailureOfType<Error.InvalidInput>();
    }

    [Fact]
    public void RemoveLineItem_NotFound_ReturnsNotFound()
    {
        var order = DraftWithProducts(("A", 1), ("B", 1));
        order.RemoveLineItem(LineItemId.NewUniqueV7()).Should().BeFailureOfType<Error.NotFound>();
    }
}
