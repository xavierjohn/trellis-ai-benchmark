namespace OrderManagement.Application.Tests;

using OrderManagement.Application.Orders;
using OrderManagement.Domain;
using Trellis.Authorization;
using Trellis.Primitives;

public class ApplicationBehaviorTests
{
    [Fact]
    public async Task Get_order_returns_not_found_when_missing()
    {
        var handler = new GetOrderByIdQueryHandler(new FakeOrders());

        var result = await handler.Handle(new GetOrderByIdQuery(OrderId.NewUniqueV7()), CancellationToken.None);

        result.Should().BeFailureOfType<Error.NotFound>();
    }

    [Fact]
    public async Task Create_product_returns_conflict_for_duplicate_sku()
    {
        var products = new FakeProducts { DuplicateSku = true };
        var handler = new CreateProductCommandHandler(products, TimeProvider.System);

        var result = await handler.Handle(
            new CreateProductCommand(ProductName.Create("Widget"), Sku.Create("ABC123"), UnitPrice.Create(12m)),
            CancellationToken.None);

        result.Should().BeFailureOfType<Error.Conflict>();
    }

    [Fact]
    public void Cancel_authorization_allows_owner_or_admin_only()
    {
        var order = Order.TryCreate(
            Customer(),
            ActorId.Create("owner"),
            [(Product(), LineItemQuantity.Create(1))],
            TimeProvider.System).Unwrap();
        var command = new CancelOrderCommand(order.Id);

        command.RequiredPermissions.Should().Contain(Permissions.OrdersCancel);
        command.Authorize(Actor.Create("owner", new HashSet<string> { Permissions.OrdersCancel }), order).Should().BeSuccess();
        command.Authorize(Actor.Create("other", new HashSet<string> { Permissions.OrdersCancel }), order).Should().BeFailureOfType<Error.Forbidden>();
        command.Authorize(Actor.Create("admin", new HashSet<string> { Permissions.OrdersCancel, Permissions.OrdersReadAll }), order).Should().BeSuccess();
    }

    private sealed class FakeOrders : IOrderRepository
    {
        public Task<Maybe<Order>> FindByIdAsync(OrderId id, CancellationToken cancellationToken) => Task.FromResult(Maybe<Order>.None);

        public Task<IReadOnlyList<Order>> ListByCustomerAsync(CustomerId customerId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Order>>([]);

        public Task<IReadOnlyList<Order>> ListOverdueAsync(DateTime asOfUtc, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Order>>([]);

        public void Add(Order order)
        {
        }
    }

    private sealed class FakeProducts : IProductRepository
    {
        public bool DuplicateSku { get; init; }

        public Task<Maybe<Product>> FindByIdAsync(ProductId id, CancellationToken cancellationToken) => Task.FromResult(Maybe<Product>.None);

        public Task<IReadOnlyList<Product>> FindByIdsAsync(IReadOnlyCollection<ProductId> ids, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Product>>([]);

        public Task<bool> ExistsBySkuAsync(Sku sku, CancellationToken cancellationToken) => Task.FromResult(DuplicateSku);

        public void Add(Product product)
        {
        }
    }

    private static Customer Customer() =>
        new(
            FirstName.Create("Test"),
            LastName.Create("Customer"),
            EmailAddress.Create($"{Guid.NewGuid():N}@example.com"),
            Maybe<PhoneNumber>.None,
            ShippingAddress.TryCreate("1 Main", "Town", "TS", "12345", "USA").Unwrap(),
            TimeProvider.System);

    private static Product Product() =>
        new(ProductName.Create("Widget"), Sku.Create(Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()), UnitPrice.Create(10m), TimeProvider.System);
}
