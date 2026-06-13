using Moq;
using OrderManagement.Application.Abstractions;
using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Products;

namespace OrderManagement.Application.Tests;

internal static class Mocks
{
    public static readonly DateTimeOffset Now = new(2026, 11, 12, 12, 0, 0, TimeSpan.Zero);

    public static IActor Actor(string id, params string[] permissions) => new Actor(id, permissions);
    public static IActor Admin(string id = "admin") => OrderManagement.Application.Abstractions.Actor.Admin(id);

    public static ShippingAddress Address() =>
        ShippingAddress.Create("1 Main", "Town", "CA", "90001", "US").Value;

    public static Customer Customer() =>
        Domain.Customers.Customer.Create("Jane", "Doe", "jane@example.com", null, Address()).Value;

    public static Product Product(string sku = "SKU001", decimal price = 10m, int stock = 100)
    {
        var p = Domain.Products.Product.Create("Widget", sku, price).Value;
        if (stock > 0) p.AddStock(stock);
        return p;
    }

    public static Order DraftOrder(Customer customer, Product product, string actorId, int qty = 1)
    {
        var items = new List<Order.LineItemRequest> { new(product.Id, product.ProductName, qty, product.UnitPrice) };
        return Order.CreateDraft(customer.Id, actorId, items, Now).Value;
    }

    public static TimeProvider Clock() => new FixedTimeProvider(Now);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
