namespace OrderManagement.Application.Orders;

using OrderManagement.Domain;

internal static class NotFound
{
    public static Error Customer(CustomerId id) =>
        new Error.NotFound(ResourceRef.For<Customer>(id)) { Detail = "Customer not found." };

    public static Error Product(ProductId id) =>
        new Error.NotFound(ResourceRef.For<Product>(id)) { Detail = "Product not found." };

    public static Error Order(OrderId id) =>
        new Error.NotFound(ResourceRef.For<Order>(id)) { Detail = "Order not found." };
}
