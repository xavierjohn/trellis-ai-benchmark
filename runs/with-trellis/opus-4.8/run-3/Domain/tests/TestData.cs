namespace Domain.Tests;

using OrderManagement.Domain;
using Trellis.Primitives;

/// <summary>Shared builders for domain tests.</summary>
internal static class TestData
{
    public static ShippingAddress Address() =>
        ShippingAddress.TryCreate("1 Main St", "Redmond", "WA", "98052", "US").Unwrap();

    public static Customer Customer(string email = "alice@example.com") =>
        OrderManagement.Domain.Customer.Create(
            FirstName.Create("Alice"),
            LastName.Create("Smith"),
            EmailAddress.Create(email),
            Maybe<PhoneNumber>.None,
            Address());

    public static Product Product(string sku = "ABC123", decimal price = 9.99m, int stock = 100) =>
        OrderManagement.Domain.Product.Create(
            ProductName.Create("Widget"),
            Sku.Create(sku),
            UnitPrice.Create(price),
            StockQuantity.Create(stock));

    public static Order DraftOrder(Product product, int quantity = 2, string actor = "actor-1")
    {
        var line = new DraftLineItem(product.Id, product.Name, Quantity.Create(quantity), product.UnitPrice);
        return Order.CreateDraft(CustomerId.NewUniqueV7(), actor, [line]).Unwrap();
    }
}
