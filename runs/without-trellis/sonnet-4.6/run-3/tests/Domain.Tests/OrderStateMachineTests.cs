using Domain.Orders;
using Domain.Products;

namespace Domain.Tests;

public class OrderStateMachineTests
{
    private static readonly TimeProvider Time = TimeProvider.System;

    private static (Order order, List<Product> products) CreateSubmittableOrder()
    {
        var order = Order.Create(Guid.NewGuid(), "actor-1", Time).Value!;
        var product = Product.Create("Widget", "WDG001", 9.99m, 100).Value!;
        order.AddLineItem(product.ProductId, product.ProductName, 2, product.UnitPrice);
        return (order, new List<Product> { product });
    }

    [Fact]
    public void DraftToSubmitted_Succeeds()
    {
        var (order, products) = CreateSubmittableOrder();
        var result = order.Submit(products, Time);
        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Submitted, order.Status);
        Assert.NotNull(order.SubmittedAt);
    }

    [Fact]
    public void SubmittedToApproved_Succeeds()
    {
        var (order, products) = CreateSubmittableOrder();
        order.Submit(products, Time);
        var result = order.Approve();
        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Approved, order.Status);
    }

    [Fact]
    public void ApprovedToShipped_Succeeds()
    {
        var (order, products) = CreateSubmittableOrder();
        order.Submit(products, Time);
        order.Approve();
        var result = order.Ship(Time);
        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Shipped, order.Status);
    }

    [Fact]
    public void ShippedToDelivered_Succeeds()
    {
        var (order, products) = CreateSubmittableOrder();
        order.Submit(products, Time);
        order.Approve();
        order.Ship(Time);
        var result = order.Deliver();
        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Delivered, order.Status);
    }

    [Fact]
    public void DraftToCancelled_Succeeds()
    {
        var order = Order.Create(Guid.NewGuid(), "actor-1", Time).Value!;
        order.AddLineItem(Guid.NewGuid(), "Widget", 2, 9.99m);
        var result = order.Cancel([]);
        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void SubmittedToCancelled_ReleasesStock()
    {
        var (order, products) = CreateSubmittableOrder();
        order.Submit(products, Time);
        Assert.Equal(98, products[0].StockQuantity);
        var result = order.Cancel(products);
        Assert.True(result.IsSuccess);
        Assert.Equal(100, products[0].StockQuantity);
    }

    [Fact]
    public void ApprovedToCancelled_ReleasesStock()
    {
        var (order, products) = CreateSubmittableOrder();
        order.Submit(products, Time);
        order.Approve();
        Assert.Equal(98, products[0].StockQuantity);
        var result = order.Cancel(products);
        Assert.True(result.IsSuccess);
        Assert.Equal(100, products[0].StockQuantity);
    }

    [Fact]
    public void InvalidTransition_ShippedToApproved_Fails()
    {
        var (order, products) = CreateSubmittableOrder();
        order.Submit(products, Time);
        order.Approve();
        order.Ship(Time);
        var result = order.Approve();
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void InvalidTransition_DeliveredToCancelled_Fails()
    {
        var (order, products) = CreateSubmittableOrder();
        order.Submit(products, Time);
        order.Approve();
        order.Ship(Time);
        order.Deliver();
        var result = order.Cancel(products);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Submit_ReservesStock()
    {
        var (order, products) = CreateSubmittableOrder();
        order.Submit(products, Time);
        Assert.Equal(98, products[0].StockQuantity);
    }

    [Fact]
    public void Submit_InsufficientStock_Fails()
    {
        var order = Order.Create(Guid.NewGuid(), "actor-1", Time).Value!;
        var product = Product.Create("Widget", "WDG001", 9.99m, 1).Value!;
        order.AddLineItem(product.ProductId, product.ProductName, 5, product.UnitPrice);
        var result = order.Submit([product], Time);
        Assert.False(result.IsSuccess);
    }
}
