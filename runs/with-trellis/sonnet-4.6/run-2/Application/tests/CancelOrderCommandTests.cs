namespace Application.Tests;

using Mediator;
using OrderManagement.Application.Customers;
using OrderManagement.Application.Orders;
using OrderManagement.Application.Products;
using OrderManagement.Domain;
using Trellis.Authorization;
using Trellis.Primitives;
using Trellis.Testing;

public class CancelOrderCommandTests
{
    private readonly ISender _sender;
    private readonly TestActorProvider _actorProvider;

    public CancelOrderCommandTests(ISender sender, TestActorProvider actorProvider)
    {
        _sender = sender;
        _actorProvider = actorProvider;
    }

    private static ShippingAddress DefaultAddress() =>
        ShippingAddress.TryCreate("1 Cancel St", "Springfield", "IL", "62701", "US").GetValueOrThrow();

    private async Task<(Customer customer, Product product, Order order)> SetupDraftOrderAsync(string emailSuffix)
    {
        var customer = (await _sender.Send(
            new CreateCustomerCommand(
                FirstName.Create("Cancel"),
                LastName.Create("Test"),
                EmailAddress.Create($"cancel{emailSuffix}@test.com"),
                Maybe<PhoneNumber>.None,
                DefaultAddress()),
            TestContext.Current.CancellationToken)).Unwrap();

        var product = (await _sender.Send(
            new CreateProductCommand(
                ProductName.Create("Cancel Product"),
                SKU.Create($"CAN{emailSuffix}"),
                UnitPrice.Create(10.00m)),
            TestContext.Current.CancellationToken)).Unwrap();

        _ = (await _sender.Send(new AddStockCommand(product.Id, Quantity.Create(50)), TestContext.Current.CancellationToken)).Unwrap();

        var order = (await _sender.Send(
            new CreateDraftOrderCommand(customer.Id, [(product.Id, Quantity.Create(2))]),
            TestContext.Current.CancellationToken)).Unwrap();

        return (customer, product, order);
    }

    [Fact]
    public async Task Cancel_draft_order_by_owner_succeeds()
    {
        var (_, _, order) = await SetupDraftOrderAsync("DRAFTOWNR");

        var result = await _sender.Send(new CancelOrderCommand(order.Id), TestContext.Current.CancellationToken);

        result.Should().BeSuccess();
        result.Unwrap().Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public async Task Cancel_draft_order_does_not_release_stock()
    {
        var (_, product, order) = await SetupDraftOrderAsync("DRAFTSTCK");
        var stockBefore = product.StockQuantity.Value;

        _ = (await _sender.Send(new CancelOrderCommand(order.Id), TestContext.Current.CancellationToken)).Unwrap();

        // Stock not reserved for Draft orders, so no release needed
        product.StockQuantity.Value.Should().Be(stockBefore);
    }

    [Fact]
    public async Task Cancel_submitted_order_succeeds()
    {
        var (_, _, order) = await SetupDraftOrderAsync("SUBMITCNL");
        _ = (await _sender.Send(new SubmitOrderCommand(order.Id), TestContext.Current.CancellationToken)).Unwrap();

        var result = await _sender.Send(new CancelOrderCommand(order.Id), TestContext.Current.CancellationToken);

        result.Should().BeSuccess();
        result.Unwrap().Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public async Task Cancel_delivered_order_fails()
    {
        var (_, _, order) = await SetupDraftOrderAsync("DLVRCNCL");

        _ = (await _sender.Send(new SubmitOrderCommand(order.Id), TestContext.Current.CancellationToken)).Unwrap();
        _ = (await _sender.Send(new ApproveOrderCommand(order.Id), TestContext.Current.CancellationToken)).Unwrap();
        _ = (await _sender.Send(new ShipOrderCommand(order.Id), TestContext.Current.CancellationToken)).Unwrap();
        _ = (await _sender.Send(new DeliverOrderCommand(order.Id), TestContext.Current.CancellationToken)).Unwrap();

        var result = await _sender.Send(new CancelOrderCommand(order.Id), TestContext.Current.CancellationToken);

        result.Should().BeFailure();
    }

    [Fact]
    public async Task Cancel_by_actor_without_permission_fails()
    {
        var (_, _, order) = await SetupDraftOrderAsync("NOPERMCNL");

        await using var scope = _actorProvider.WithActor("other-user", Permissions.OrdersCreate);
        var result = await _sender.Send(new CancelOrderCommand(order.Id), TestContext.Current.CancellationToken);
        result.Should().BeFailure();
    }
}
