using OrderManagement.Api.Domain;
using Xunit;

namespace OrderManagement.Tests.Domain;

public class OrderDomainTests
{
    private static readonly DateTime _now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly TimeProvider _timeProvider = new FakeTimeProvider(_now);

    private static (Order order, Product product) CreateOrderWithProduct(string sku = "PROD001")
    {
        var product = Product.Create($"Product {sku}", sku, 10.00m);
        product.AddStock(100);
        var customerId = Guid.NewGuid();
        var order = Order.Create(customerId, "actor-1", _now);
        var lineItem = LineItem.Create(order.Id, product.Id, product.ProductName, 1, product.UnitPrice);
        order.AddLineItem(lineItem);
        return (order, product);
    }

    [Fact]
    public void Create_NewOrder_HasDraftStatus()
    {
        var (order, _) = CreateOrderWithProduct();

        Assert.Equal(OrderStatus.Draft, order.Status);
        Assert.Single(order.LineItems);
        Assert.Equal(_now, order.CreatedAt);
    }

    [Fact]
    public void AddLineItem_ToDraftOrder_Succeeds()
    {
        var (order, _) = CreateOrderWithProduct("PROD001");
        var product2 = Product.Create("Product 2", "PROD002", 20.00m);
        var lineItem2 = LineItem.Create(order.Id, product2.Id, product2.ProductName, 2, product2.UnitPrice);

        order.AddLineItem(lineItem2);

        Assert.Equal(2, order.LineItems.Count);
    }

    [Fact]
    public void AddLineItem_DuplicateProduct_ThrowsValidationException()
    {
        var (order, product) = CreateOrderWithProduct("PROD001");
        var duplicateLineItem = LineItem.Create(order.Id, product.Id, product.ProductName, 5, product.UnitPrice);

        var ex = Assert.Throws<DomainValidationException>(() => order.AddLineItem(duplicateLineItem));
        Assert.Contains("already in this order", ex.Message);
    }

    [Fact]
    public void AddLineItem_ToNonDraftOrder_ThrowsValidationException()
    {
        var (order, _) = CreateOrderWithProduct();
        order.Submit(_timeProvider);

        var product2 = Product.Create("Product 2", "PROD002", 20.00m);
        var lineItem2 = LineItem.Create(order.Id, product2.Id, product2.ProductName, 1, product2.UnitPrice);

        var ex = Assert.Throws<DomainValidationException>(() => order.AddLineItem(lineItem2));
        Assert.Contains("Draft", ex.Message);
    }

    [Fact]
    public void RemoveLineItem_WithMultipleItems_Succeeds()
    {
        var (order, _) = CreateOrderWithProduct("PROD001");
        var product2 = Product.Create("Product 2", "PROD002", 20.00m);
        var lineItem2 = LineItem.Create(order.Id, product2.Id, product2.ProductName, 1, product2.UnitPrice);
        order.AddLineItem(lineItem2);

        var lineItemIdToRemove = order.LineItems[0].Id;
        order.RemoveLineItem(lineItemIdToRemove);

        Assert.Single(order.LineItems);
    }

    [Fact]
    public void RemoveLineItem_LastItem_ThrowsValidationException()
    {
        var (order, _) = CreateOrderWithProduct();

        var ex = Assert.Throws<DomainValidationException>(() =>
            order.RemoveLineItem(order.LineItems[0].Id));

        Assert.Contains("last line item", ex.Message);
    }

    [Fact]
    public void RemoveLineItem_FromNonDraftOrder_ThrowsValidationException()
    {
        var (order, _) = CreateOrderWithProduct("PROD001");
        var product2 = Product.Create("Product 2", "PROD002", 20.00m);
        var lineItem2 = LineItem.Create(order.Id, product2.Id, product2.ProductName, 1, product2.UnitPrice);
        order.AddLineItem(lineItem2);
        order.Submit(_timeProvider);

        var ex = Assert.Throws<DomainValidationException>(() =>
            order.RemoveLineItem(order.LineItems[0].Id));

        Assert.Contains("Draft", ex.Message);
    }

    [Fact]
    public void RemoveLineItem_NonExistentId_ThrowsNotFoundException()
    {
        var (order, _) = CreateOrderWithProduct("PROD001");
        var product2 = Product.Create("Product 2", "PROD002", 20.00m);
        var lineItem2 = LineItem.Create(order.Id, product2.Id, product2.ProductName, 1, product2.UnitPrice);
        order.AddLineItem(lineItem2);

        Assert.Throws<NotFoundException>(() => order.RemoveLineItem(Guid.NewGuid()));
    }

    [Fact]
    public void OrderTotal_CalculatesCorrectly()
    {
        var customerId = Guid.NewGuid();
        var order = Order.Create(customerId, "actor-1", _now);
        var p1 = Product.Create("P1", "SKU001", 10.00m);
        var p2 = Product.Create("P2", "SKU002", 25.50m);
        order.AddLineItem(LineItem.Create(order.Id, p1.Id, p1.ProductName, 2, p1.UnitPrice));
        order.AddLineItem(LineItem.Create(order.Id, p2.Id, p2.ProductName, 3, p2.UnitPrice));

        // 2 * 10 + 3 * 25.5 = 20 + 76.5 = 96.5
        Assert.Equal(96.5m, order.OrderTotal);
    }
}
