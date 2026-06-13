namespace Domain.Tests;

using OrderManagement.Domain;
using Trellis.Primitives;

/// <summary>Fixed-time <see cref="TimeProvider"/> for deterministic domain tests.</summary>
internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

/// <summary>Factory helpers that build valid domain objects for tests.</summary>
internal static class Build
{
    public static ShippingAddress Address() =>
        ShippingAddress.TryCreate("1 Main St", "Springfield", "IL", "62701", "USA").Unwrap();

    public static Customer Customer(string email = "jane@example.com", string? phone = null) =>
        OrderManagement.Domain.Customer.Create(
            FirstName.TryCreate("Jane").Unwrap(),
            LastName.TryCreate("Doe").Unwrap(),
            EmailAddress.TryCreate(email).Unwrap(),
            phone is null ? Maybe<PhoneNumber>.None : Maybe.From(PhoneNumber.TryCreate(phone).Unwrap()),
            Address());

    public static Product Product(string sku = "WIDGET01", decimal price = 9.99m, int stock = 0)
    {
        var product = OrderManagement.Domain.Product.TryCreate(
            ProductName.TryCreate("Widget").Unwrap(),
            Sku.TryCreate(sku).Unwrap(),
            MonetaryAmount.Create(price)).Unwrap();
        if (stock > 0)
            product.AddStock(StockAddition.TryCreate(stock).Unwrap()).Unwrap();
        return product;
    }

    public static LineItem LineItem(Product product, int quantity = 1) =>
        OrderManagement.Domain.LineItem.Create(
            product.Id,
            product.Name,
            Quantity.TryCreate(quantity).Unwrap(),
            product.UnitPrice);

    public static Order DraftOrder(string actorId = "actor-1", params LineItem[] lineItems)
    {
        var items = lineItems.Length > 0 ? lineItems : [LineItem(Product(stock: 10))];
        return Order.TryCreate(CustomerId.NewUniqueV7(), actorId, items).Unwrap();
    }
}
