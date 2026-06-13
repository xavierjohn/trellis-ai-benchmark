using OrderManagement.Domain.Aggregates;
using OrderManagement.Domain.Enums;
using OrderManagement.Domain.Exceptions;
using Xunit;

namespace OrderManagement.Tests.Domain;

public class OrderTests
{
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid ProductId1 = Guid.NewGuid();
    private static readonly Guid ProductId2 = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private static Order CreateDraftOrder()
    {
        var order = Order.Create(CustomerId, "actor-1", Now);
        order.AddLineItem(ProductId1, "Widget", 2, 10m);
        return order;
    }

    [Fact]
    public void Create_SetsStatusToDraft()
    {
        var order = Order.Create(CustomerId, "actor-1", Now);
        Assert.Equal(OrderStatus.Draft, order.Status);
        Assert.Equal(CustomerId, order.CustomerId);
        Assert.Equal("actor-1", order.CreatedByActorId);
    }

    [Fact]
    public void AddLineItem_AddsSuccessfully()
    {
        var order = Order.Create(CustomerId, "actor-1", Now);
        order.AddLineItem(ProductId1, "Widget", 5, 9.99m);

        Assert.Single(order.LineItems);
        Assert.Equal(ProductId1, order.LineItems[0].ProductId);
        Assert.Equal(5, order.LineItems[0].Quantity);
        Assert.Equal(9.99m, order.LineItems[0].UnitPrice);
    }

    [Fact]
    public void AddLineItem_DuplicateProduct_ThrowsValidation()
    {
        var order = CreateDraftOrder();
        var ex = Assert.Throws<ValidationException>(() =>
            order.AddLineItem(ProductId1, "Widget", 1, 10m));
        Assert.Contains("already in this order", ex.Message);
    }

    [Fact]
    public void AddLineItem_QuantityTooLow_ThrowsValidation()
    {
        var order = Order.Create(CustomerId, "actor-1", Now);
        Assert.Throws<ValidationException>(() =>
            order.AddLineItem(ProductId1, "Widget", 0, 10m));
    }

    [Fact]
    public void AddLineItem_QuantityTooHigh_ThrowsValidation()
    {
        var order = Order.Create(CustomerId, "actor-1", Now);
        Assert.Throws<ValidationException>(() =>
            order.AddLineItem(ProductId1, "Widget", 1000, 10m));
    }

    [Fact]
    public void RemoveLineItem_WithTwoItems_RemovesOne()
    {
        var order = CreateDraftOrder();
        order.AddLineItem(ProductId2, "Gadget", 1, 20m);
        var lineItemId = order.LineItems[0].Id;

        order.RemoveLineItem(lineItemId);

        Assert.Single(order.LineItems);
        Assert.DoesNotContain(order.LineItems, li => li.Id == lineItemId);
    }

    [Fact]
    public void RemoveLineItem_LastItem_ThrowsValidation()
    {
        var order = CreateDraftOrder();
        var lineItemId = order.LineItems[0].Id;

        var ex = Assert.Throws<ValidationException>(() => order.RemoveLineItem(lineItemId));
        Assert.Contains("last line item", ex.Message);
    }

    [Fact]
    public void RemoveLineItem_NotFoundId_ThrowsNotFound()
    {
        var order = CreateDraftOrder();
        order.AddLineItem(ProductId2, "Gadget", 1, 20m);

        Assert.Throws<NotFoundException>(() => order.RemoveLineItem(Guid.NewGuid()));
    }

    [Fact]
    public void OrderTotal_CalculatesCorrectly()
    {
        var order = Order.Create(CustomerId, "actor-1", Now);
        order.AddLineItem(ProductId1, "Widget", 3, 10m);
        order.AddLineItem(ProductId2, "Gadget", 2, 25m);

        Assert.Equal(80m, order.OrderTotal); // 3*10 + 2*25 = 30 + 50
    }

    [Fact]
    public void RemoveLineItem_NotInDraft_ThrowsValidation()
    {
        var order = CreateDraftOrder();
        order.AddLineItem(ProductId2, "Gadget", 1, 20m);

        var stockCalls = new List<(Guid, int)>();
        order.Submit(Now, (pid, qty) => stockCalls.Add((pid, qty)));

        var lineItemId = order.LineItems[0].Id;
        var ex = Assert.Throws<ValidationException>(() => order.RemoveLineItem(lineItemId));
        Assert.Contains("Draft", ex.Message);
    }
}
