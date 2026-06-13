namespace OrderManagement.Application.Tests;

using OrderManagement.Domain;
using Trellis.Primitives;

/// <summary>Factory helpers for building valid domain objects in application tests.</summary>
internal static class TestData
{
    public static Customer Customer(string email = "jane@example.com") =>
        new(
            FirstName.Create("Jane"),
            LastName.Create("Doe"),
            EmailAddress.Create(email),
            Maybe<PhoneNumber>.None,
            ShippingAddress.TryCreate("1 Main St", "Town", "CA", "90001", "US").Unwrap());

    public static Product Product(string sku = "WIDGET01", decimal price = 10m, int stock = 0)
    {
        var product = new Product(ProductName.Create("Widget"), Sku.Create(sku), UnitPrice.Create(price));
        if (stock > 0)
            product.AddStock(stock).Discard();
        return product;
    }

    public static Order OrderFor(Customer customer, Product product, string createdBy = "sales-1", int quantity = 2)
    {
        var lines = new[]
        {
            new OrderLineInput(product.Id, product.Name, Quantity.Create(quantity), product.UnitPrice),
        };
        return Order.Create(customer.Id, createdBy, lines).Unwrap();
    }
}
