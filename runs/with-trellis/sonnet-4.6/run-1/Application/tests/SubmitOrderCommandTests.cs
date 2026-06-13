namespace Application.Tests;

using Mediator;
using OrderManagement.Application.Customers;
using OrderManagement.Application.Orders;
using OrderManagement.Application.Products;
using OrderManagement.Domain;
using Trellis.Authorization;

public class SubmitOrderCommandTests
{
    private readonly ISender _sender;
    private readonly TestActorProvider _actorProvider;

    public SubmitOrderCommandTests(ISender sender, TestActorProvider actorProvider)
    {
        _sender = sender;
        _actorProvider = actorProvider;
    }

    [Fact]
    public async Task Submit_order_with_insufficient_stock_returns_invalid_input()
    {
        var address = ShippingAddress.TryCreate("1 Main St", "Seattle", "WA", "98101", "US").Unwrap();
        await using var scope = _actorProvider.WithActor(
            "owner-1",
            Permissions.CustomersCreate,
            Permissions.ProductsCreate,
            Permissions.OrdersCreate,
            Permissions.OrdersSubmit);

        var customer = (await _sender.Send(new CreateCustomerCommand(
            FirstName.Create("Jane"),
            LastName.Create("Doe"),
            Trellis.Primitives.EmailAddress.Create("submit@example.com"),
            Maybe<Trellis.Primitives.PhoneNumber>.None,
            address), TestContext.Current.CancellationToken)).Unwrap();
        var product = (await _sender.Send(new CreateProductCommand(
            ProductName.Create("Widget"),
            Sku.Create("SKU789"),
            10m), TestContext.Current.CancellationToken)).Unwrap();
        var order = (await _sender.Send(
            CreateDraftOrderCommand.TryCreate(customer.Id, [new CreateDraftOrderInput(product.Id, Quantity.Create(2))]).Unwrap(),
            TestContext.Current.CancellationToken)).Unwrap();

        var result = await _sender.Send(new SubmitOrderCommand(order.Id), TestContext.Current.CancellationToken);

        result.Should().BeFailureOfType<Error.InvalidInput>();
    }
}
