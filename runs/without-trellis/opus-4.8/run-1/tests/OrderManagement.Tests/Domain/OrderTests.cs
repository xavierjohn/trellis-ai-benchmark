using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Products;
using static OrderManagement.Tests.Domain.DomainFixtures;

namespace OrderManagement.Tests.Domain;

public class OrderTests
{
    private readonly Guid _customer = Guid.NewGuid();

    [Fact]
    public void Create_WithLineItems_Succeeds()
    {
        var p = ProductWithStock("SKU001", 10m, 5);

        var result = Order.Create(_customer, "actor-1",
            new[] { new Order.NewLineItem(p.Id, p.ProductName, 2, p.UnitPrice) }, DateTimeOffset.UnixEpoch);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Draft, result.Value.Status);
        Assert.Single(result.Value.LineItems);
        Assert.Equal(20m, result.Value.OrderTotal);
    }

    [Fact]
    public void Create_WithNoLineItems_Fails()
    {
        var result = Order.Create(_customer, "actor-1", Array.Empty<Order.NewLineItem>(), DateTimeOffset.UnixEpoch);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Create_WithDuplicateProduct_Fails()
    {
        var p = ProductWithStock("SKU001", 10m, 5);
        var lines = new[]
        {
            new Order.NewLineItem(p.Id, p.ProductName, 1, p.UnitPrice),
            new Order.NewLineItem(p.Id, p.ProductName, 2, p.UnitPrice),
        };

        var result = Order.Create(_customer, "actor-1", lines, DateTimeOffset.UnixEpoch);
        Assert.True(result.IsFailure);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1000)]
    public void Create_WithOutOfRangeQuantity_Fails(int qty)
    {
        var p = ProductWithStock("SKU001", 10m, 5);
        var result = Order.Create(_customer, "actor-1",
            new[] { new Order.NewLineItem(p.Id, p.ProductName, qty, p.UnitPrice) }, DateTimeOffset.UnixEpoch);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void AddLineItem_ToDraft_Succeeds()
    {
        var p1 = ProductWithStock("SKU001", 10m, 5);
        var p2 = ProductWithStock("SKU002", 5m, 5);
        var order = DraftOrder(_customer, "actor-1", (p1, 1));

        var result = order.AddLineItem(p2.Id, p2.ProductName, 3, p2.UnitPrice);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, order.LineItems.Count);
    }

    [Fact]
    public void AddLineItem_DuplicateProduct_Fails()
    {
        var p1 = ProductWithStock("SKU001", 10m, 5);
        var order = DraftOrder(_customer, "actor-1", (p1, 1));

        var result = order.AddLineItem(p1.Id, p1.ProductName, 1, p1.UnitPrice);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void RemoveLineItem_WhenMultiple_Succeeds()
    {
        var p1 = ProductWithStock("SKU001", 10m, 5);
        var p2 = ProductWithStock("SKU002", 5m, 5);
        var order = DraftOrder(_customer, "actor-1", (p1, 1), (p2, 1));
        var toRemove = order.LineItems[0].Id;

        var result = order.RemoveLineItem(toRemove);

        Assert.True(result.IsSuccess);
        Assert.Single(order.LineItems);
    }

    [Fact]
    public void RemoveLineItem_LastOne_Fails()
    {
        var p1 = ProductWithStock("SKU001", 10m, 5);
        var order = DraftOrder(_customer, "actor-1", (p1, 1));

        var result = order.RemoveLineItem(order.LineItems[0].Id);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void RemoveLineItem_NotFound_ReturnsNotFound()
    {
        var p1 = ProductWithStock("SKU001", 10m, 5);
        var p2 = ProductWithStock("SKU002", 5m, 5);
        var order = DraftOrder(_customer, "actor-1", (p1, 1), (p2, 1));

        var result = order.RemoveLineItem(Guid.NewGuid());
        Assert.True(result.IsFailure);
        Assert.Equal(OrderManagement.Domain.Common.ErrorKind.NotFound, result.Error!.Kind);
    }

    [Fact]
    public void AddLineItem_WhenNotDraft_Fails()
    {
        var p1 = ProductWithStock("SKU001", 10m, 5);
        var p2 = ProductWithStock("SKU002", 5m, 5);
        var order = DraftOrder(_customer, "actor-1", (p1, 1));
        order.Submit(Dict(p1), DateTimeOffset.UnixEpoch);

        var result = order.AddLineItem(p2.Id, p2.ProductName, 1, p2.UnitPrice);
        Assert.True(result.IsFailure);
    }
}
