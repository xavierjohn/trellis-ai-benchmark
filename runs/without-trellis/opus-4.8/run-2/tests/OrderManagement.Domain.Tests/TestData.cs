using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Products;

namespace OrderManagement.Domain.Tests;

internal static class TestData
{
    public static readonly DateTimeOffset Now = new(2026, 11, 12, 12, 0, 0, TimeSpan.Zero);

    public static ShippingAddress Address() =>
        ShippingAddress.Create("1 Main St", "Townsville", "CA", "90001", "US").Value;

    public static Product ProductWithStock(string sku = "SKU001", decimal price = 10m, int stock = 100)
    {
        var product = Product.Create("Widget", sku, price).Value;
        if (stock > 0) product.AddStock(stock);
        return product;
    }

    public static Order DraftOrder(Product product, int qty = 2, Guid? customerId = null, string actor = "actor-1")
    {
        var items = new List<Order.LineItemRequest>
        {
            new(product.Id, product.ProductName, qty, product.UnitPrice)
        };
        return Order.CreateDraft(customerId ?? Guid.NewGuid(), actor, items, Now).Value;
    }
}
