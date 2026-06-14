namespace OrderManagement.Application.Tests;

using OrderManagement.Application.Orders;
using OrderManagement.Domain;
using Trellis.Authorization;

public sealed class AuthorizationTests
{
    [Fact]
    public void Cancel_authorization_allows_owner_and_admin_only()
    {
        var product = Product.TryCreate(ProductName.Create("Widget"), Sku.Create("ABC123"), UnitPrice.Create(10m)).Unwrap();
        var order = Order.TryCreate(CustomerId.NewUniqueV7(), ActorId.Create("owner"), [(product, OrderQuantity.Create(1))]).Unwrap();
        var command = new CancelOrderCommand(order.Id);

        command.Authorize(Actor.Create("owner", new HashSet<string> { Permissions.OrdersCancel }), order).Should().BeSuccess();
        command.Authorize(Actor.Create("admin", new HashSet<string> { Permissions.OrdersCancel, Permissions.OrdersReadAll }), order).Should().BeSuccess();
        command.Authorize(Actor.Create("other", new HashSet<string> { Permissions.OrdersCancel }), order).Should().BeFailureOfType<Error.Forbidden>();
    }
}
