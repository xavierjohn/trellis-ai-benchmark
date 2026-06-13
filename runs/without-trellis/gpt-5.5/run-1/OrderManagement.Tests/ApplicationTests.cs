using OrderManagement.Api;

namespace OrderManagement.Tests;

public sealed class ApplicationTests
{
    private readonly InMemoryCustomerRepository _customers = new();
    private readonly InMemoryProductRepository _products = new();
    private readonly InMemoryOrderRepository _orders = new();
    private readonly InMemoryUnitOfWork _uow = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 12, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Command_succeeds_with_required_permission_and_fails_without_it()
    {
        var service = Service();
        var permitted = new Actor("sales-1", new HashSet<string> { Permissions.CustomersCreate });
        var denied = new Actor("reader", new HashSet<string>());
        var request = new CreateCustomerRequest("Ada", "Lovelace", "ada@example.com", null, AddressDto());

        var success = await service.CreateCustomerAsync(permitted, request);
        var forbidden = await service.CreateCustomerAsync(denied, request with { Email = "other@example.com" });

        Assert.True(success.IsSuccess);
        Assert.True(_uow.Saved);
        Assert.Equal(ErrorKind.Forbidden, forbidden.Error?.Kind);
    }

    [Fact]
    public async Task Cancel_resource_authorization_allows_owner_and_admin_but_rejects_non_owner()
    {
        var service = Service();
        var order = DraftOrder("owner");
        await _orders.AddAsync(order);

        var nonOwner = await service.CancelAsync(new Actor("other", new HashSet<string> { Permissions.OrdersCancel }), order.Id);
        var owner = await service.CancelAsync(new Actor("owner", new HashSet<string> { Permissions.OrdersCancel }), order.Id);

        var adminOrder = DraftOrder("owner");
        await _orders.AddAsync(adminOrder);
        var admin = await service.CancelAsync(new Actor("admin", new HashSet<string> { Permissions.OrdersCancel, Permissions.OrdersReadAll }), adminOrder.Id);

        Assert.Equal(ErrorKind.Forbidden, nonOwner.Error?.Kind);
        Assert.True(owner.IsSuccess);
        Assert.True(admin.IsSuccess);
    }

    [Fact]
    public async Task Query_returns_not_found_when_entity_does_not_exist()
    {
        var result = await Service().GetOrderAsync(new Actor("reader", new HashSet<string> { Permissions.OrdersRead }), Guid.NewGuid());

        Assert.Equal(ErrorKind.NotFound, result.Error?.Kind);
    }

    private OrderManagementService Service() => new(_customers, _products, _orders, _uow, _time);

    private static Order DraftOrder(string actorId) =>
        new(Guid.NewGuid(), actorId, [new OrderLineItem(Guid.NewGuid(), "Keyboard", 1, 10m)], new DateTimeOffset(2026, 11, 12, 12, 0, 0, TimeSpan.Zero));

    private static AddressDto AddressDto() => new("1 Main", "Seattle", "WA", "98101", "USA");
}

internal sealed class InMemoryCustomerRepository : ICustomerRepository
{
    private readonly Dictionary<Guid, Customer> _customers = [];
    public Task<Customer?> GetAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(_customers.GetValueOrDefault(id));
    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) => Task.FromResult(_customers.Values.Any(c => c.Email == email.ToLowerInvariant()));
    public Task AddAsync(Customer customer, CancellationToken cancellationToken = default) { _customers[customer.Id] = customer; return Task.CompletedTask; }
}

internal sealed class InMemoryProductRepository : IProductRepository
{
    private readonly Dictionary<Guid, Product> _products = [];
    public Task<Product?> GetAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(_products.GetValueOrDefault(id));
    public Task<List<Product>> GetManyAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) => Task.FromResult(_products.Values.Where(p => ids.Contains(p.Id)).ToList());
    public Task<bool> SkuExistsAsync(string sku, CancellationToken cancellationToken = default) => Task.FromResult(_products.Values.Any(p => p.Sku == sku));
    public Task AddAsync(Product product, CancellationToken cancellationToken = default) { _products[product.Id] = product; return Task.CompletedTask; }
}

internal sealed class InMemoryOrderRepository : IOrderRepository
{
    private readonly Dictionary<Guid, Order> _orders = [];
    public Task<Order?> GetAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(_orders.GetValueOrDefault(id));
    public Task<List<Order>> ListByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default) => Task.FromResult(_orders.Values.Where(o => o.CustomerId == customerId).ToList());
    public Task<List<Order>> ListSubmittedBeforeAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default) => Task.FromResult(_orders.Values.Where(o => o.Status == OrderStatus.Submitted && o.SubmittedAt < cutoff).ToList());
    public Task AddAsync(Order order, CancellationToken cancellationToken = default) { _orders[order.Id] = order; return Task.CompletedTask; }
}

internal sealed class InMemoryUnitOfWork : IUnitOfWork
{
    public bool Saved { get; private set; }
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) { Saved = true; return Task.CompletedTask; }
}

public sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    private DateTimeOffset _utcNow = utcNow;
    public override DateTimeOffset GetUtcNow() => _utcNow;
    public void SetUtcNow(DateTimeOffset utcNow) => _utcNow = utcNow;
}
