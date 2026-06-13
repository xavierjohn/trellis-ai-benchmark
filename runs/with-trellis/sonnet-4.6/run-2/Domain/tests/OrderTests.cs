namespace Domain.Tests;

using OrderManagement.Domain;

public class OrderTests
{
    private static readonly CustomerId s_customerId = CustomerId.NewUniqueV7();
    private static string TestActorId => "actor-1";

    private static Order CreateDraftOrder()
    {
        var order = new Order(s_customerId, TestActorId);
        order.AddLineItem(ProductId.NewUniqueV7(), ProductName.Create("Test Product"), Quantity.Create(2), UnitPrice.Create(10.00m)).Should().BeSuccess();
        return order;
    }

    [Fact]
    public void Constructor_creates_draft_order()
    {
        var order = CreateDraftOrder();

        order.Status.Should().Be(OrderStatus.Draft);
        order.CustomerId.Should().Be(s_customerId);
        order.CreatedByActorId.Should().Be(TestActorId);
        order.LineItems.Should().HaveCount(1);
        order.OrderTotal.Should().Be(20.00m);
        order.SubmittedAt.Should().BeNone();
        order.ShippedAt.Should().BeNone();
    }

    [Fact]
    public void Submit_from_draft_transitions_to_submitted()
    {
        var order = CreateDraftOrder();
        var result = order.Submit(TimeProvider.System);
        result.Should().BeSuccess();
        order.Status.Should().Be(OrderStatus.Submitted);
        order.SubmittedAt.Should().HaveValue();
    }

    [Fact]
    public void Submit_raises_OrderSubmittedEvent()
    {
        var order = CreateDraftOrder();
        order.AcceptChanges();
        order.Submit(TimeProvider.System).Should().BeSuccess();
        order.UncommittedEvents().Should().ContainSingle()
            .Which.Should().BeOfType<OrderSubmittedEvent>();
    }

    [Fact]
    public void Approve_from_submitted_transitions_to_approved()
    {
        var order = CreateDraftOrder();
        order.Submit(TimeProvider.System).Should().BeSuccess();
        var result = order.Approve(TimeProvider.System);
        result.Should().BeSuccess();
        order.Status.Should().Be(OrderStatus.Approved);
    }

    [Fact]
    public void Ship_from_approved_transitions_to_shipped()
    {
        var order = CreateDraftOrder();
        order.Submit(TimeProvider.System).Should().BeSuccess();
        order.Approve(TimeProvider.System).Should().BeSuccess();
        var result = order.Ship(TimeProvider.System);
        result.Should().BeSuccess();
        order.Status.Should().Be(OrderStatus.Shipped);
        order.ShippedAt.Should().HaveValue();
    }

    [Fact]
    public void Deliver_from_shipped_transitions_to_delivered()
    {
        var order = CreateDraftOrder();
        order.Submit(TimeProvider.System).Should().BeSuccess();
        order.Approve(TimeProvider.System).Should().BeSuccess();
        order.Ship(TimeProvider.System).Should().BeSuccess();
        var result = order.Deliver(TimeProvider.System);
        result.Should().BeSuccess();
        order.Status.Should().Be(OrderStatus.Delivered);
    }

    [Fact]
    public void Cancel_from_draft_succeeds_and_does_not_require_release()
    {
        var order = CreateDraftOrder();
        var result = order.Cancel(TimeProvider.System);
        result.Should().BeSuccess();
        order.Status.Should().Be(OrderStatus.Cancelled);
        result.Unwrap().releaseStock.Should().BeFalse();
    }

    [Fact]
    public void Cancel_from_submitted_requires_stock_release()
    {
        var order = CreateDraftOrder();
        order.Submit(TimeProvider.System).Should().BeSuccess();
        var result = order.Cancel(TimeProvider.System);
        result.Should().BeSuccess();
        result.Unwrap().releaseStock.Should().BeTrue();
        result.Unwrap().lineItems.Should().HaveCount(1);
    }

    [Fact]
    public void Cancel_from_delivered_fails()
    {
        var order = CreateDraftOrder();
        order.Submit(TimeProvider.System).Should().BeSuccess();
        order.Approve(TimeProvider.System).Should().BeSuccess();
        order.Ship(TimeProvider.System).Should().BeSuccess();
        order.Deliver(TimeProvider.System).Should().BeSuccess();
        var result = order.Cancel(TimeProvider.System);
        result.Should().BeFailure();
    }

    [Fact]
    public void AddLineItem_to_draft_order_succeeds()
    {
        var order = CreateDraftOrder();
        var newProductId = ProductId.NewUniqueV7();
        var result = order.AddLineItem(newProductId, ProductName.Create("New Product"), Quantity.Create(3), UnitPrice.Create(5.00m));
        result.Should().BeSuccess();
        order.LineItems.Should().HaveCount(2);
        order.OrderTotal.Should().Be(10.00m * 2 + 5.00m * 3);
    }

    [Fact]
    public void AddLineItem_to_submitted_order_fails()
    {
        var order = CreateDraftOrder();
        order.Submit(TimeProvider.System).Should().BeSuccess();
        var result = order.AddLineItem(ProductId.NewUniqueV7(), ProductName.Create("Extra"), Quantity.Create(1), UnitPrice.Create(1.00m));
        result.Should().BeFailure();
    }

    [Fact]
    public void AddLineItem_duplicate_product_fails()
    {
        var order = CreateDraftOrder();
        var existingProductId = order.LineItems[0].ProductId;
        var result = order.AddLineItem(existingProductId, ProductName.Create("Duplicate"), Quantity.Create(1), UnitPrice.Create(1.00m));
        result.Should().BeFailure();
    }

    [Fact]
    public void RemoveLineItem_from_draft_order_with_multiple_items_succeeds()
    {
        var order = CreateDraftOrder();
        order.AddLineItem(ProductId.NewUniqueV7(), ProductName.Create("Second"), Quantity.Create(1), UnitPrice.Create(5.00m)).Should().BeSuccess();
        var result = order.RemoveLineItem(order.LineItems[0].Id);
        result.Should().BeSuccess();
        order.LineItems.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveLineItem_last_item_fails()
    {
        var order = CreateDraftOrder();
        var result = order.RemoveLineItem(order.LineItems[0].Id);
        result.Should().BeFailure();
    }

    [Fact]
    public void Approve_from_draft_fails()
    {
        var order = CreateDraftOrder();
        var result = order.Approve(TimeProvider.System);
        result.Should().BeFailure();
    }
}
