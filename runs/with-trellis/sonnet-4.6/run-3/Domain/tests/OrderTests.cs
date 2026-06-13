namespace Domain.Tests;

using OrderManagement.Domain;
using Trellis.Testing;

public class OrderTests
{
    private static CustomerId MakeCustomerId() => CustomerId.Create(Guid.NewGuid());

    private static LineItem MakeLineItem(int quantity = 1) =>
        new(
            ProductId.Create(Guid.NewGuid()),
            ProductName.TryCreate("Product A").Unwrap(),
            quantity,
            UnitPrice.TryCreate(10.00m).Unwrap());

    private static Order MakeOrder(int lineItemCount = 1)
    {
        var items = Enumerable.Range(0, lineItemCount).Select(_ => MakeLineItem()).ToList();
        return new Order(MakeCustomerId(), "actor-1", items);
    }

    [Fact]
    public void Create_order_with_line_items_is_in_draft_status()
    {
        var order = MakeOrder();
        order.Status.Should().Be(OrderStatus.Draft);
        order.LineItems.Should().HaveCount(1);
    }

    [Fact]
    public void AddLineItem_to_draft_order_succeeds()
    {
        var order = MakeOrder();
        var newItem = MakeLineItem();
        order.AddLineItem(newItem).Should().BeSuccess();
        order.LineItems.Should().HaveCount(2);
    }

    [Fact]
    public void AddLineItem_duplicate_product_fails()
    {
        var productId = ProductId.Create(Guid.NewGuid());
        var item1 = new LineItem(productId, ProductName.TryCreate("Dup").Unwrap(), 1, UnitPrice.TryCreate(1m).Unwrap());
        var item2 = new LineItem(productId, ProductName.TryCreate("Dup").Unwrap(), 2, UnitPrice.TryCreate(1m).Unwrap());
        var order = new Order(MakeCustomerId(), "actor-1", [item1]);

        order.AddLineItem(item2).Should().BeFailure();
    }

    [Fact]
    public void RemoveLineItem_from_order_with_multiple_items_succeeds()
    {
        var order = MakeOrder(2);
        var lineItemId = order.LineItems[0].Id;
        order.RemoveLineItem(lineItemId).Should().BeSuccess();
        order.LineItems.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveLineItem_last_item_fails()
    {
        var order = MakeOrder(1);
        var lineItemId = order.LineItems[0].Id;
        order.RemoveLineItem(lineItemId).Should().BeFailure();
    }

    [Fact]
    public void Submit_draft_order_transitions_to_submitted()
    {
        var order = MakeOrder();
        order.Submit(TimeProvider.System).Should().BeSuccess();
        order.Status.Should().Be(OrderStatus.Submitted);
        order.SubmittedAt.Should().HaveValue();
    }

    [Fact]
    public void Approve_submitted_order_transitions_to_approved()
    {
        var order = MakeOrder();
        order.Submit(TimeProvider.System).Should().BeSuccess();
        order.Approve(TimeProvider.System).Should().BeSuccess();
        order.Status.Should().Be(OrderStatus.Approved);
    }

    [Fact]
    public void Ship_approved_order_transitions_to_shipped()
    {
        var order = MakeOrder();
        order.Submit(TimeProvider.System).Should().BeSuccess();
        order.Approve(TimeProvider.System).Should().BeSuccess();
        order.Ship(TimeProvider.System).Should().BeSuccess();
        order.Status.Should().Be(OrderStatus.Shipped);
        order.ShippedAt.Should().HaveValue();
    }

    [Fact]
    public void Deliver_shipped_order_transitions_to_delivered()
    {
        var order = MakeOrder();
        order.Submit(TimeProvider.System).Should().BeSuccess();
        order.Approve(TimeProvider.System).Should().BeSuccess();
        order.Ship(TimeProvider.System).Should().BeSuccess();
        order.Deliver(TimeProvider.System).Should().BeSuccess();
        order.Status.Should().Be(OrderStatus.Delivered);
    }

    [Fact]
    public void Cancel_draft_order_transitions_to_cancelled()
    {
        var order = MakeOrder();
        order.Cancel(TimeProvider.System).Should().BeSuccess();
        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_submitted_order_transitions_to_cancelled()
    {
        var order = MakeOrder();
        order.Submit(TimeProvider.System).Should().BeSuccess();
        order.Cancel(TimeProvider.System).Should().BeSuccess();
        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_approved_order_transitions_to_cancelled()
    {
        var order = MakeOrder();
        order.Submit(TimeProvider.System).Should().BeSuccess();
        order.Approve(TimeProvider.System).Should().BeSuccess();
        order.Cancel(TimeProvider.System).Should().BeSuccess();
        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_shipped_order_fails_invalid_transition()
    {
        var order = MakeOrder();
        order.Submit(TimeProvider.System).Should().BeSuccess();
        order.Approve(TimeProvider.System).Should().BeSuccess();
        order.Ship(TimeProvider.System).Should().BeSuccess();
        order.Cancel(TimeProvider.System).Should().BeFailure();
    }

    [Fact]
    public void Invalid_transition_draft_to_approve_fails()
    {
        var order = MakeOrder();
        order.Approve(TimeProvider.System).Should().BeFailure();
    }

    [Fact]
    public void Order_total_is_sum_of_line_item_totals()
    {
        var customerId = MakeCustomerId();
        var item1 = new LineItem(ProductId.Create(Guid.NewGuid()), ProductName.TryCreate("A").Unwrap(), 2, UnitPrice.TryCreate(10m).Unwrap());
        var item2 = new LineItem(ProductId.Create(Guid.NewGuid()), ProductName.TryCreate("B").Unwrap(), 3, UnitPrice.TryCreate(5m).Unwrap());
        var order = new Order(customerId, "actor-1", [item1, item2]);

        order.OrderTotal.Should().Be(35m);
    }
}
