namespace Application.Tests;

using Mediator;
using OrderManagement.Application;
using OrderManagement.Domain;
using Trellis.Testing;

public class CancelOrderAuthorizationTests(
    ISender sender,
    TestActorProvider actorProvider,
    FakeRepository<Order, OrderId> orders)
{
    private Order SeedDraftOrder(string ownerId)
    {
        var order = Build.DraftOrder(CustomerId.NewUniqueV7(), ownerId, Build.Product());
        orders.Add(order);
        return order;
    }

    [Fact]
    public async Task Cancel_by_owner_succeeds()
    {
        var order = SeedDraftOrder("owner-1");
        await using var _ = actorProvider.WithActor("owner-1", Permissions.OrdersCancel);

        var result = await sender.Send(new CancelOrderCommand(order.Id), TestContext.Current.CancellationToken);

        result.Should().BeSuccess();
        result.Unwrap().Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public async Task Cancel_by_non_owner_returns_forbidden()
    {
        var order = SeedDraftOrder("owner-1");
        await using var _ = actorProvider.WithActor("intruder-2", Permissions.OrdersCancel);

        var result = await sender.Send(new CancelOrderCommand(order.Id), TestContext.Current.CancellationToken);

        result.Should().BeFailureOfType<Error.Forbidden>();
    }

    [Fact]
    public async Task Cancel_by_admin_with_read_all_succeeds()
    {
        var order = SeedDraftOrder("owner-1");
        await using var _ = actorProvider.WithActor("admin-9", Permissions.OrdersCancel, Permissions.OrdersReadAll);

        var result = await sender.Send(new CancelOrderCommand(order.Id), TestContext.Current.CancellationToken);

        result.Should().BeSuccess();
    }
}
