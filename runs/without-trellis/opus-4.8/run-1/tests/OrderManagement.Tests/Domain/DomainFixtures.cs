using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Products;

namespace OrderManagement.Tests.Domain;

/// <summary>Shared builders for order/product domain tests.</summary>
internal static class DomainFixtures
{
    public static Product ProductWithStock(string sku, decimal price, int stock)
    {
        var product = Product.Create($"Product {sku}", sku, price).Value;
        if (stock > 0) product.AddStock(stock);
        return product;
    }

    public static Order DraftOrder(Guid customerId, string actorId, params (Product product, int qty)[] lines)
    {
        var newLines = lines
            .Select(l => new Order.NewLineItem(l.product.Id, l.product.ProductName, l.qty, l.product.UnitPrice))
            .ToList();
        return Order.Create(customerId, actorId, newLines, DateTimeOffset.UnixEpoch).Value;
    }

    public static IReadOnlyDictionary<Guid, Product> Dict(params Product[] products)
        => products.ToDictionary(p => p.Id);
}
