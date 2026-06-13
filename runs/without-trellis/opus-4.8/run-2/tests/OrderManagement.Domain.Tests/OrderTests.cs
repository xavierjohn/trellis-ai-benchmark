using OrderManagement.Domain.Common;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Products;
using Xunit;

namespace OrderManagement.Domain.Tests;

public class OrderTests
{
    [Fact]
    public void CreateDraft_with_line_items_succeeds()
    {
        var product = TestData.ProductWithStock();
        var order = TestData.DraftOrder(product, qty: 3);

        Assert.Equal(OrderStatus.Draft, order.Status);
        Assert.Single(order.LineItems);
        Assert.Equal(product.UnitPrice * 3, order.OrderTotal);
    }

    [Fact]
    public void CreateDraft_with_no_line_items_fails()
    {
        var result = Order.CreateDraft(Guid.NewGuid(), "actor-1", new List<Order.LineItemRequest>(), TestData.Now);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
    }

    [Fact]
    public void CreateDraft_with_duplicate_product_fails()
    {
        var product = TestData.ProductWithStock();
        var items = new List<Order.LineItemRequest>
        {
            new(product.Id, product.ProductName, 1, product.UnitPrice),
            new(product.Id, product.ProductName, 2, product.UnitPrice),
        };

        var result = Order.CreateDraft(Guid.NewGuid(), "actor-1", items, TestData.Now);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1000)]
    public void CreateDraft_with_out_of_range_quantity_fails(int qty)
    {
        var product = TestData.ProductWithStock();
        var items = new List<Order.LineItemRequest> { new(product.Id, product.ProductName, qty, product.UnitPrice) };

        var result = Order.CreateDraft(Guid.NewGuid(), "actor-1", items, TestData.Now);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
    }

    [Fact]
    public void AddLineItem_adds_new_product()
    {
        var product1 = TestData.ProductWithStock("SKU001");
        var product2 = Product.Create("Gadget", "SKU002", 5m).Value;
        var order = TestData.DraftOrder(product1);

        var result = order.AddLineItem(product2.Id, product2.ProductName, 1, product2.UnitPrice);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, order.LineItems.Count);
    }

    [Fact]
    public void AddLineItem_duplicate_product_fails()
    {
        var product = TestData.ProductWithStock();
        var order = TestData.DraftOrder(product);

        var result = order.AddLineItem(product.Id, product.ProductName, 1, product.UnitPrice);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
    }

    [Fact]
    public void AddLineItem_on_non_draft_fails()
    {
        var product = TestData.ProductWithStock();
        var order = TestData.DraftOrder(product);
        order.Submit(new[] { product }, TestData.Now);

        var other = Product.Create("Gadget", "SKU002", 5m).Value;
        var result = order.AddLineItem(other.Id, other.ProductName, 1, other.UnitPrice);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
    }

    [Fact]
    public void RemoveLineItem_removes_when_more_than_one()
    {
        var product1 = TestData.ProductWithStock("SKU001");
        var product2 = Product.Create("Gadget", "SKU002", 5m).Value;
        var order = TestData.DraftOrder(product1);
        order.AddLineItem(product2.Id, product2.ProductName, 1, product2.UnitPrice);
        var toRemove = order.LineItems[0].Id;

        var result = order.RemoveLineItem(toRemove);

        Assert.True(result.IsSuccess);
        Assert.Single(order.LineItems);
    }

    [Fact]
    public void RemoveLineItem_last_item_fails()
    {
        var product = TestData.ProductWithStock();
        var order = TestData.DraftOrder(product);
        var lineItemId = order.LineItems[0].Id;

        var result = order.RemoveLineItem(lineItemId);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
    }

    [Fact]
    public void RemoveLineItem_unknown_id_returns_not_found()
    {
        var product1 = TestData.ProductWithStock("SKU001");
        var product2 = Product.Create("Gadget", "SKU002", 5m).Value;
        var order = TestData.DraftOrder(product1);
        order.AddLineItem(product2.Id, product2.ProductName, 1, product2.UnitPrice);

        var result = order.RemoveLineItem(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.NotFound, result.Error!.Kind);
    }

    [Fact]
    public void OrderTotal_sums_all_line_items()
    {
        var product1 = TestData.ProductWithStock("SKU001", price: 10m);
        var product2 = Product.Create("Gadget", "SKU002", 2.5m).Value;
        var order = TestData.DraftOrder(product1, qty: 2); // 20
        order.AddLineItem(product2.Id, product2.ProductName, 4, product2.UnitPrice); // 10

        Assert.Equal(30m, order.OrderTotal);
    }
}
