namespace Application.Tests;

using OrderManagement.Domain;
using Trellis.Primitives;

/// <summary>Factory helpers that build valid domain objects for application-layer tests.</summary>
internal static class Build
{
    public static ShippingAddress Address() =>
        ShippingAddress.TryCreate("1 Main St", "Springfield", "IL", "62701", "USA").Unwrap();

    public static Customer Customer(string email = "jane@example.com") =>
        OrderManagement.Domain.Customer.Create(
            FirstName.TryCreate("Jane").Unwrap(),
            LastName.TryCreate("Doe").Unwrap(),
            EmailAddress.TryCreate(email).Unwrap(),
            Maybe<PhoneNumber>.None,
            Address());

    public static Product Product(string sku = "WIDGET01", decimal price = 9.99m, int stock = 10)
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

    public static Order DraftOrder(CustomerId customerId, string actorId, Product product) =>
        Order.TryCreate(customerId, actorId, [LineItem(product)]).Unwrap();
}
