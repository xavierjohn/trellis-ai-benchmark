using OrderManagement.Domain.Common;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Products;

namespace OrderManagement.Tests.Domain;

public class OrderTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-06-12T00:00:00Z");

    private static Product Prod(string sku, decimal price, int stock)
        => Product.Create($"Product {sku}", sku, price, stock).Value;

    private static Order DraftWith(params (Product Product, int Qty)[] lines)
    {
        var requests = lines
            .Select(l => (new Order.LineRequest(l.Product.Id, l.Qty), l.Product))
            .ToList();
        return Order.CreateDraft(Guid.NewGuid(), "actor-1", requests, Now).Value;
    }

    private static IReadOnlyDictionary<Guid, Product> Map(params Product[] products)
        => products.ToDictionary(p => p.Id);

    [Fact]
    public void CreateDraft_CapturesUnitPriceAndName()
    {
        var product = Prod("WIDGET01", 9.99m, 10);

        var order = DraftWith((product, 2));

        Assert.Equal(OrderStatus.Draft, order.Status);
        Assert.Single(order.LineItems);
        Assert.Equal(9.99m, order.LineItems[0].UnitPrice);
        Assert.Equal("Product WIDGET01", order.LineItems[0].ProductName);
        Assert.Equal(19.98m, order.OrderTotal);
    }

    [Fact]
    public void CreateDraft_WithNoLines_Fails()
    {
        var result = Order.CreateDraft(Guid.NewGuid(), "actor-1", [], Now);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error!.Type);
    }

    [Fact]
    public void CreateDraft_WithDuplicateProduct_Fails()
    {
        var product = Prod("WIDGET01", 9.99m, 10);
        var requests = new List<(Order.LineRequest, Product)>
        {
            (new Order.LineRequest(product.Id, 1), product),
            (new Order.LineRequest(product.Id, 2), product)
        };

        var result = Order.CreateDraft(Guid.NewGuid(), "actor-1", requests, Now);

        Assert.True(result.IsFailure);
        Assert.Contains("multiple line items", result.Error!.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1000)]
    public void CreateDraft_WithQuantityOutOfRange_Fails(int qty)
    {
        var product = Prod("WIDGET01", 9.99m, 2000);
        var requests = new List<(Order.LineRequest, Product)> { (new Order.LineRequest(product.Id, qty), product) };

        var result = Order.CreateDraft(Guid.NewGuid(), "actor-1", requests, Now);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void AddLineItem_NewProduct_Succeeds()
    {
        var p1 = Prod("WIDGET01", 9.99m, 10);
        var p2 = Prod("WIDGET02", 4.50m, 10);
        var order = DraftWith((p1, 1));

        var result = order.AddLineItem(p2, 3);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, order.LineItems.Count);
    }

    [Fact]
    public void AddLineItem_DuplicateProduct_Fails()
    {
        var p1 = Prod("WIDGET01", 9.99m, 10);
        var order = DraftWith((p1, 1));

        var result = order.AddLineItem(p1, 3);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void AddLineItem_WhenNotDraft_Fails()
    {
        var p1 = Prod("WIDGET01", 9.99m, 10);
        var p2 = Prod("WIDGET02", 4.50m, 10);
        var order = DraftWith((p1, 1));
        order.Submit(Map(p1), Now);

        var result = order.AddLineItem(p2, 1);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void RemoveLineItem_Succeeds()
    {
        var p1 = Prod("WIDGET01", 9.99m, 10);
        var p2 = Prod("WIDGET02", 4.50m, 10);
        var order = DraftWith((p1, 1), (p2, 1));
        var toRemove = order.LineItems[0].Id;

        var result = order.RemoveLineItem(toRemove);

        Assert.True(result.IsSuccess);
        Assert.Single(order.LineItems);
    }

    [Fact]
    public void RemoveLineItem_LastItem_Fails()
    {
        var p1 = Prod("WIDGET01", 9.99m, 10);
        var order = DraftWith((p1, 1));

        var result = order.RemoveLineItem(order.LineItems[0].Id);

        Assert.True(result.IsFailure);
        Assert.Contains("last line item", result.Error!.Message);
    }

    [Fact]
    public void RemoveLineItem_NotFound_ReturnsNotFound()
    {
        var p1 = Prod("WIDGET01", 9.99m, 10);
        var p2 = Prod("WIDGET02", 4.50m, 10);
        var order = DraftWith((p1, 1), (p2, 1));

        var result = order.RemoveLineItem(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }
}
