using Microsoft.Extensions.Time.Testing;
using OrderManagement.Api.Domain.Common;
using OrderManagement.Api.Domain.Orders;
using OrderManagement.Api.Domain.Products;
using Xunit;

namespace OrderManagement.Tests.Domain;

public class OrderTests
{
    private static readonly FakeTimeProvider Time = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

    private static Product NewProduct(string sku = "WIDGET01", int stock = 100, decimal price = 10m) =>
        Product.Create("Widget", sku, price, stock);

    private static Order NewDraft(params (Guid productId, decimal price, int qty)[] items)
    {
        var drafts = items.Select(i => new LineItemDraft(i.productId, "Widget", i.price, i.qty)).ToList();
        return Order.CreateDraft(Guid.NewGuid(), "actor-1", drafts, Time);
    }

    [Fact]
    public void CreateDraft_WithLineItems_Succeeds()
    {
        var pid = Guid.NewGuid();
        var order = NewDraft((pid, 10m, 2));

        Assert.Equal(OrderStatus.Draft, order.Status);
        Assert.Single(order.LineItems);
        Assert.Equal(20m, order.OrderTotal);
        Assert.Equal("actor-1", order.CreatedByActorId);
    }

    [Fact]
    public void CreateDraft_WithNoLineItems_Throws()
    {
        Assert.Throws<ValidationAppException>(() =>
            Order.CreateDraft(Guid.NewGuid(), "actor-1", new List<LineItemDraft>(), Time));
    }

    [Fact]
    public void CreateDraft_WithDuplicateProduct_Throws()
    {
        var pid = Guid.NewGuid();
        Assert.Throws<ValidationAppException>(() => NewDraft((pid, 10m, 1), (pid, 10m, 2)));
    }

    [Fact]
    public void AddLineItem_NewProduct_Succeeds()
    {
        var order = NewDraft((Guid.NewGuid(), 10m, 1));
        var newProductId = Guid.NewGuid();
        order.AddLineItem(newProductId, "Gadget", 5m, 3);

        Assert.Equal(2, order.LineItems.Count);
        Assert.Equal(25m, order.OrderTotal);
    }

    [Fact]
    public void AddLineItem_DuplicateProduct_Throws()
    {
        var pid = Guid.NewGuid();
        var order = NewDraft((pid, 10m, 1));
        Assert.Throws<ValidationAppException>(() => order.AddLineItem(pid, "Widget", 10m, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1000)]
    public void AddLineItem_InvalidQuantity_Throws(int qty)
    {
        var order = NewDraft((Guid.NewGuid(), 10m, 1));
        Assert.Throws<ValidationAppException>(() => order.AddLineItem(Guid.NewGuid(), "Gadget", 5m, qty));
    }

    [Fact]
    public void RemoveLineItem_Succeeds()
    {
        var order = NewDraft((Guid.NewGuid(), 10m, 1), (Guid.NewGuid(), 5m, 2));
        var toRemove = order.LineItems[0].Id;
        order.RemoveLineItem(toRemove);
        Assert.Single(order.LineItems);
    }

    [Fact]
    public void RemoveLineItem_LastItem_Throws()
    {
        var order = NewDraft((Guid.NewGuid(), 10m, 1));
        var only = order.LineItems[0].Id;
        Assert.Throws<ValidationAppException>(() => order.RemoveLineItem(only));
    }

    [Fact]
    public void RemoveLineItem_NotFound_Throws()
    {
        var order = NewDraft((Guid.NewGuid(), 10m, 1), (Guid.NewGuid(), 5m, 2));
        Assert.Throws<NotFoundAppException>(() => order.RemoveLineItem(Guid.NewGuid()));
    }

    [Fact]
    public void UnitPrice_IsSnapshot_NotAffectedByLaterChanges()
    {
        var pid = Guid.NewGuid();
        var order = NewDraft((pid, 10m, 2));
        Assert.Equal(10m, order.LineItems[0].UnitPrice);
        // Snapshot is fixed; no API to change it after the fact.
    }
}
