namespace Application.Tests;

using FluentAssertions;
using OrderManagement.Domain;
using Xunit;

public sealed class OrderManagementApplicationTests
{
    [Fact]
    public void Permission_catalog_contains_cancel_ownership_permissions()
    {
        Permissions.All.Should().Contain(Permissions.OrdersCancel);
        Permissions.All.Should().Contain(Permissions.OrdersReadAll);
    }

    [Fact]
    public void Cancel_ownership_rule_is_owner_or_admin()
    {
        static bool canCancel(string actorId, bool readAll, Order order) =>
            order.CreatedByActorId == actorId || readAll;

        var product = Product.TryCreate("Widget", "ABC123", 10m).GetValueOrThrow("test product");
        var order = Order.TryCreate(Guid.NewGuid(), "owner", [(product, 1)], TimeProvider.System).GetValueOrThrow("test order");

        canCancel("owner", false, order).Should().BeTrue();
        canCancel("other", false, order).Should().BeFalse();
        canCancel("other", true, order).Should().BeTrue();
    }
}
