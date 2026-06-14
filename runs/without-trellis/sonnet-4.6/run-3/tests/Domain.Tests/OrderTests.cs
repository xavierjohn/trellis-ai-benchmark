using Domain.Orders;

namespace Domain.Tests;

public class OrderTests
{
    private static readonly TimeProvider Time = TimeProvider.System;
    private static readonly Guid CustomerId = Guid.NewGuid();

    [Fact]
    public void Create_ValidOrder_Succeeds()
    {
        var result = Order.Create(CustomerId, "actor-1", Time);
        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Draft, result.Value!.Status);
    }

    [Fact]
    public void AddLineItem_ValidItem_Succeeds()
    {
        var order = Order.Create(CustomerId, "actor-1", Time).Value!;
        var result = order.AddLineItem(Guid.NewGuid(), "Widget", 2, 9.99m);
        Assert.True(result.IsSuccess);
        Assert.Single(order.LineItems);
    }

    [Fact]
    public void AddLineItem_DuplicateProduct_Fails()
    {
        var order = Order.Create(CustomerId, "actor-1", Time).Value!;
        var productId = Guid.NewGuid();
        order.AddLineItem(productId, "Widget", 2, 9.99m);
        var result = order.AddLineItem(productId, "Widget", 3, 9.99m);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void RemoveLineItem_LastItem_Fails()
    {
        var order = Order.Create(CustomerId, "actor-1", Time).Value!;
        order.AddLineItem(Guid.NewGuid(), "Widget", 2, 9.99m);
        var lineItemId = order.LineItems[0].LineItemId;
        var result = order.RemoveLineItem(lineItemId);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void RemoveLineItem_WithMultiple_Succeeds()
    {
        var order = Order.Create(CustomerId, "actor-1", Time).Value!;
        order.AddLineItem(Guid.NewGuid(), "Widget", 2, 9.99m);
        order.AddLineItem(Guid.NewGuid(), "Gadget", 1, 4.99m);
        var lineItemId = order.LineItems[0].LineItemId;
        var result = order.RemoveLineItem(lineItemId);
        Assert.True(result.IsSuccess);
        Assert.Single(order.LineItems);
    }
}
