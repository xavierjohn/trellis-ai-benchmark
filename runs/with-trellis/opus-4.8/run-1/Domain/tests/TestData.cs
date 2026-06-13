namespace OrderManagement.Domain.Tests;

using OrderManagement.Domain;
using Trellis.Primitives;

/// <summary>Factory helpers for building valid domain value objects in tests.</summary>
internal static class TestData
{
    public static FirstName FirstName(string value = "Jane") => Domain.FirstName.Create(value);

    public static LastName LastName(string value = "Doe") => Domain.LastName.Create(value);

    public static EmailAddress Email(string value = "jane@example.com") => EmailAddress.Create(value);

    public static PhoneNumber Phone(string value = "+14155550100") => PhoneNumber.Create(value);

    public static ProductName ProductName(string value = "Widget") => Domain.ProductName.Create(value);

    public static Sku Sku(string value = "WIDGET01") => Domain.Sku.Create(value);

    public static UnitPrice UnitPrice(decimal value = 9.99m) => Domain.UnitPrice.Create(value);

    public static Quantity Quantity(int value = 1) => Domain.Quantity.Create(value);

    public static ShippingAddress Address() =>
        ShippingAddress.TryCreate("1 Main St", "Town", "CA", "90001", "US").Unwrap();

    public static Customer Customer() =>
        new(FirstName(), LastName(), Email(), Maybe<PhoneNumber>.None, Address());

    public static Product Product(decimal price = 9.99m) =>
        new(ProductName(), Sku(), UnitPrice(price));

    public static Order DraftOrder(int quantity = 2, decimal price = 9.99m)
    {
        var lines = new[]
        {
            new OrderLineInput(ProductId.NewUniqueV7(), ProductName(), Quantity(quantity), UnitPrice(price)),
        };
        return Order.Create(CustomerId.NewUniqueV7(), "actor-1", lines).Unwrap();
    }
}
