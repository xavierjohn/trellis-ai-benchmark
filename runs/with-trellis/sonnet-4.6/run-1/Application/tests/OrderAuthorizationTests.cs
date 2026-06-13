namespace Application.Tests;

using Mediator;
using OrderManagement.Application.Customers;
using OrderManagement.Application.Orders;
using OrderManagement.Application.Products;
using OrderManagement.Domain;
using Trellis.Authorization;

public class OrderAuthorizationTests
{
    private readonly ISender _sender;
    private readonly TestActorProvider _actorProvider;

    public OrderAuthorizationTests(ISender sender, TestActorProvider actorProvider)
    {
        _sender = sender;
        _actorProvider = actorProvider;
    }

    [Fact]
    public async Task Cancel_other_users_order_returns_forbidden()
    {
        var address = ShippingAddress.TryCreate("1 Main St", "Seattle", "WA", "98101", "US").Unwrap();
        await using var ownerScope = _actorProvider.WithActor(
            "owner-1",
            Permissions.CustomersCreate,
            Permissions.ProductsCreate,
            Permissions.ProductsManageStock,
            Permissions.OrdersCreate,
            Permissions.OrdersCancel);

        var customer = (await _sender.Send(new CreateCustomerCommand(
            FirstName.Create("Jane"),
            LastName.Create("Doe"),
            Trellis.Primitives.EmailAddress.Create("owner@example.com"),
            Maybe<Trellis.Primitives.PhoneNumber>.None,
            address), TestContext.Current.CancellationToken)).Unwrap();
        var product = (await _sender.Send(new CreateProductCommand(
            ProductName.Create("Widget"),
            Sku.Create("SKU123"),
            10m), TestContext.Current.CancellationToken)).Unwrap();
        (await _sender.Send(new AddStockCommand(product.Id, 10), TestContext.Current.CancellationToken)).Should().BeSuccess();
        var order = (await _sender.Send(
            CreateDraftOrderCommand.TryCreate(customer.Id, [new CreateDraftOrderInput(product.Id, Quantity.Create(1))]).Unwrap(),
            TestContext.Current.CancellationToken)).Unwrap();

        await using var otherUserScope = _actorProvider.WithActor("other-user", Permissions.OrdersCancel);
        var result = await _sender.Send(new CancelOrderCommand(order.Id), TestContext.Current.CancellationToken);

        result.Should().BeFailureOfType<Error.Forbidden>();
    }
}
