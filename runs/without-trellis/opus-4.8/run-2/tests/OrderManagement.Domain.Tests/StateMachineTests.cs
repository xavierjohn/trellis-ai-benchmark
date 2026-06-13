using OrderManagement.Domain.Common;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Products;
using Xunit;

namespace OrderManagement.Domain.Tests;

public class StateMachineTests
{
    private static (Order order, Product product) Approved(int stock = 100, int qty = 2)
    {
        var product = TestData.ProductWithStock(stock: stock);
        var order = TestData.DraftOrder(product, qty);
        order.Submit(new[] { product }, TestData.Now);
        order.Approve(TestData.Now);
        return (order, product);
    }

    [Fact]
    public void Submit_reserves_stock_and_sets_submitted_at()
    {
        var product = TestData.ProductWithStock(stock: 10);
        var order = TestData.DraftOrder(product, qty: 3);

        var result = order.Submit(new[] { product }, TestData.Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Submitted, order.Status);
        Assert.Equal(TestData.Now, order.SubmittedAt);
        Assert.Equal(7, product.StockQuantity);
        Assert.Contains(order.DomainEvents, e => e is OrderSubmittedEvent);
    }

    [Fact]
    public void Submit_with_insufficient_stock_fails_and_reserves_nothing()
    {
        var product = TestData.ProductWithStock(stock: 2);
        var order = TestData.DraftOrder(product, qty: 5);

        var result = order.Submit(new[] { product }, TestData.Now);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
        Assert.Equal(OrderStatus.Draft, order.Status);
        Assert.Equal(2, product.StockQuantity);
    }

    [Fact]
    public void Approve_from_submitted_succeeds()
    {
        var product = TestData.ProductWithStock(stock: 10);
        var order = TestData.DraftOrder(product, qty: 1);
        order.Submit(new[] { product }, TestData.Now);

        var result = order.Approve(TestData.Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Approved, order.Status);
    }

    [Fact]
    public void Ship_sets_shipped_at_and_deliver_completes()
    {
        var (order, _) = Approved();

        var ship = order.Ship(TestData.Now);
        Assert.True(ship.IsSuccess);
        Assert.Equal(OrderStatus.Shipped, order.Status);
        Assert.Equal(TestData.Now, order.ShippedAt);

        var deliver = order.Deliver(TestData.Now);
        Assert.True(deliver.IsSuccess);
        Assert.Equal(OrderStatus.Delivered, order.Status);
    }

    [Fact]
    public void Cancel_from_draft_succeeds_without_releasing_stock()
    {
        var product = TestData.ProductWithStock(stock: 10);
        var order = TestData.DraftOrder(product, qty: 2);

        var result = order.Cancel(new[] { product }, TestData.Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(10, product.StockQuantity); // never reserved
    }

    [Fact]
    public void Cancel_from_submitted_releases_stock()
    {
        var product = TestData.ProductWithStock(stock: 10);
        var order = TestData.DraftOrder(product, qty: 4);
        order.Submit(new[] { product }, TestData.Now);
        Assert.Equal(6, product.StockQuantity);

        var result = order.Cancel(new[] { product }, TestData.Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(10, product.StockQuantity); // released
    }

    [Fact]
    public void Cancel_from_approved_releases_stock()
    {
        var (order, product) = Approved(stock: 10, qty: 4);
        Assert.Equal(6, product.StockQuantity);

        var result = order.Cancel(new[] { product }, TestData.Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(10, product.StockQuantity);
    }

    // ---- Invalid transitions ----

    [Fact]
    public void Approve_from_draft_is_invalid()
    {
        var product = TestData.ProductWithStock();
        var order = TestData.DraftOrder(product);

        var result = order.Approve(TestData.Now);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
        Assert.Equal(OrderStatus.Draft, order.Status);
    }

    [Fact]
    public void Submit_twice_is_invalid()
    {
        var product = TestData.ProductWithStock(stock: 10);
        var order = TestData.DraftOrder(product, qty: 1);
        order.Submit(new[] { product }, TestData.Now);

        var result = order.Submit(new[] { product }, TestData.Now);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
    }

    [Fact]
    public void Ship_from_submitted_is_invalid()
    {
        var product = TestData.ProductWithStock(stock: 10);
        var order = TestData.DraftOrder(product, qty: 1);
        order.Submit(new[] { product }, TestData.Now);

        var result = order.Ship(TestData.Now);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
    }

    [Fact]
    public void Deliver_from_approved_is_invalid()
    {
        var (order, _) = Approved();

        var result = order.Deliver(TestData.Now);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
    }

    [Fact]
    public void Cancel_from_shipped_is_invalid()
    {
        var (order, product) = Approved(stock: 10, qty: 2);
        order.Ship(TestData.Now);

        var result = order.Cancel(new[] { product }, TestData.Now);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
        Assert.Equal(OrderStatus.Shipped, order.Status);
    }

    [Fact]
    public void Cancel_from_delivered_is_invalid()
    {
        var (order, product) = Approved(stock: 10, qty: 2);
        order.Ship(TestData.Now);
        order.Deliver(TestData.Now);

        var result = order.Cancel(new[] { product }, TestData.Now);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
    }
}
