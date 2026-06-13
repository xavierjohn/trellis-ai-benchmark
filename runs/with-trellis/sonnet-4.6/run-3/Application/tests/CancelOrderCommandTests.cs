namespace Application.Tests;

using Mediator;
using OrderManagement.Application.Customers;
using OrderManagement.Application.Orders;
using OrderManagement.Application.Products;
using OrderManagement.Domain;
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

    private async Task<Order> CreateOrderAsActor(string actorId)
    {
        var customerCmd = new CreateCustomerCommand(
            FirstName.TryCreate("Test").Unwrap(),
            LastName.TryCreate("User").Unwrap(),
            Email.TryCreate($"{actorId}@test.com").Unwrap(),
            Maybe<PhoneNumber>.None,
            ShippingAddress.TryCreate("123 St", "City", "ST", "12345", "US").Unwrap());
        var customerResult = await _sender.Send(customerCmd, TestContext.Current.CancellationToken);
        customerResult.Should().BeSuccess();

        var productCmd = new CreateProductCommand(
            ProductName.TryCreate("Widget").Unwrap(),
            Sku.TryCreate($"WGT{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}").Unwrap(),
            UnitPrice.TryCreate(10m).Unwrap());
        var productResult = await _sender.Send(productCmd, TestContext.Current.CancellationToken);
        productResult.Should().BeSuccess();

        await using var scope = _actorProvider.WithActor(
            actorId,
            Permissions.OrdersCreate,
            Permissions.OrdersSubmit,
            Permissions.OrdersCancel,
            Permissions.OrdersRead);
        var orderCmd = CreateDraftOrderCommand.TryCreate(
            customerResult.Unwrap().Id,
            [new OrderLineItemInput(productResult.Unwrap().Id, 1)]);
        orderCmd.Should().BeSuccess();
        var orderResult = await _sender.Send(orderCmd.Unwrap(), TestContext.Current.CancellationToken);
        orderResult.Should().BeSuccess();
        return orderResult.Unwrap();
    }

    [Fact]
    public async Task Cancel_own_order_succeeds()
    {
        var order = await CreateOrderAsActor("owner-1");

        await using var _ = _actorProvider.WithActor("owner-1", Permissions.OrdersCancel);
        var result = await _sender.Send(new CancelOrderCommand(order.Id), TestContext.Current.CancellationToken);

        result.Should().BeSuccess();
        result.Unwrap().Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public async Task Cancel_another_users_order_returns_forbidden()
    {
        var order = await CreateOrderAsActor("owner-2");

        await using var _ = _actorProvider.WithActor("other-user", Permissions.OrdersCancel);
        var result = await _sender.Send(new CancelOrderCommand(order.Id), TestContext.Current.CancellationToken);

        result.Should().BeFailureOfType<Error.Forbidden>();
    }

    [Fact]
    public async Task Cancel_by_admin_with_read_all_succeeds_regardless_of_owner()
    {
        var order = await CreateOrderAsActor("owner-3");

        await using var _ = _actorProvider.WithActor("admin", Permissions.OrdersCancel, Permissions.OrdersReadAll);
        var result = await _sender.Send(new CancelOrderCommand(order.Id), TestContext.Current.CancellationToken);

        result.Should().BeSuccess();
        result.Unwrap().Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public async Task Cancel_nonexistent_order_returns_not_found()
    {
        await using var _ = _actorProvider.WithActor("test-user", Permissions.OrdersCancel, Permissions.OrdersReadAll);
        var result = await _sender.Send(new CancelOrderCommand(OrderId.Create(Guid.NewGuid())), TestContext.Current.CancellationToken);

        result.Should().BeFailureOfType<Error.NotFound>();
    }
}
