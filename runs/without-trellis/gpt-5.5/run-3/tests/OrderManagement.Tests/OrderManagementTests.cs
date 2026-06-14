using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using global::OrderManagement.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace OrderManagement.Tests;

public sealed class OrderManagementTests
{
    private const string Version = "?api-version=2026-11-12";

    [Fact]
    public void Domain_rules_cover_validation_line_items_stock_and_state_machine()
    {
        var time = new MutableTimeProvider();
        var address = new ShippingAddress("1 Main", "Austin", "TX", "78701", "US");
        Assert.Throws<ValidationProblemException>(() => global::OrderManagement.Api.Customer.Create("", "Lovelace", "bad", null, address));
        var product = Product.Create("Keyboard", "KEY123", 10m);
        var mouse = Product.Create("Mouse", "MOU123", 5m);
        product.AddStock(10);
        Assert.Throws<ValidationProblemException>(() => product.ReserveStock(11));
        var order = Order.Create(Guid.NewGuid(), "actor-1", [(product, 2)], time);
        Assert.Equal(20m, order.OrderTotal);
        Assert.Throws<ValidationProblemException>(() => order.AddLineItem(product, 1));
        Assert.Throws<ValidationProblemException>(() => order.RemoveLineItem(order.LineItems.Single().Id));
        order.AddLineItem(mouse, 1);
        order.RemoveLineItem(order.LineItems.First(x => x.ProductId == mouse.Id).Id);
        Assert.Throws<ValidationProblemException>(() => order.Approve());
        order.Submit([product], time);
        Assert.Equal(8, product.StockQuantity);
        order.Approve();
        order.Cancel([product]);
        Assert.Equal(10, product.StockQuantity);
    }

    [Fact]
    public async Task Service_authorization_cancel_ownership_not_found_and_overdue_work()
    {
        var time = new MutableTimeProvider(new DateTimeOffset(2026, 06, 01, 0, 0, 0, TimeSpan.Zero));
        await using var fixture = await ServiceFixture.CreateAsync(new Actor("owner", Permissions.All.ToHashSet()), time);
        var customer = await fixture.Service.CreateCustomerAsync(MakeCustomer("svc@example.com"), CancellationToken.None);
        var product = await fixture.Service.CreateProductAsync(new CreateProductRequest("Keyboard", "KEY123", 10m), CancellationToken.None);
        await fixture.Service.AddStockAsync(product.Id, new AddStockRequest(10), CancellationToken.None);
        var old = await fixture.Service.CreateOrderAsync(new CreateOrderRequest(customer.Id, [new OrderLineItemRequest(product.Id, 1)]), CancellationToken.None);
        await fixture.Service.SubmitOrderAsync(old.Id, CancellationToken.None);
        time.Advance(TimeSpan.FromDays(8));
        var recent = await fixture.Service.CreateOrderAsync(new CreateOrderRequest(customer.Id, [new OrderLineItemRequest(product.Id, 1)]), CancellationToken.None);
        await fixture.Service.SubmitOrderAsync(recent.Id, CancellationToken.None);

        Assert.Single(await fixture.Service.ListOverdueOrdersAsync(CancellationToken.None));
        fixture.ActorProvider.Actor = new Actor("other", new HashSet<string>([Permissions.OrdersCancel]));
        await Assert.ThrowsAsync<ForbiddenProblemException>(() => fixture.Service.CancelOrderAsync(old.Id, CancellationToken.None));
        fixture.ActorProvider.Actor = new Actor("reader", new HashSet<string>([Permissions.OrdersRead]));
        await Assert.ThrowsAsync<NotFoundProblemException>(() => fixture.Service.GetOrderAsync(Guid.NewGuid(), CancellationToken.None));
        fixture.ActorProvider.Actor = new Actor("blocked", new HashSet<string>());
        await Assert.ThrowsAsync<ForbiddenProblemException>(() => fixture.Service.CreateCustomerAsync(MakeCustomer("blocked@example.com"), CancellationToken.None));
    }

    [Fact]
    public async Task Api_integration_covers_health_errors_lifecycle_cancel_and_overdue()
    {
        using var factory = new TestAppFactory();
        var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/customers", MakeCustomer("noversion@example.com"))).StatusCode);

        client.DefaultRequestHeaders.Add("X-Test-Actor", """{"id":"blocked","permissions":[]}""");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/customers" + Version, MakeCustomer("denied@example.com"))).StatusCode);
        client.DefaultRequestHeaders.Remove("X-Test-Actor");

        var customerId = await CreateCustomer(client, "api@example.com");
        var duplicate = await client.PostAsJsonAsync("/api/customers" + Version, MakeCustomer("api@example.com"));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("application/problem+json", duplicate.Content.Headers.ContentType?.MediaType);

        var productId = await CreateProduct(client, "API123");
        await client.PostAsJsonAsync($"/api/products/{productId}/stock-additions{Version}", new AddStockRequest(10));
        var order = await PostJson(client, "/api/orders" + Version, new CreateOrderRequest(customerId, [new OrderLineItemRequest(productId, 2)]));
        var orderId = order["id"]!.GetValue<string>();
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/orders/{orderId}/submission{Version}", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/orders/{orderId}/approval{Version}", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/orders/{orderId}/shipment{Version}", null)).StatusCode);
        var delivered = await PostJson(client, $"/api/orders/{orderId}/delivery{Version}", new { });
        Assert.Equal("Delivered", delivered["status"]!.GetValue<string>());

        client.DefaultRequestHeaders.Add("X-Test-Actor", """{"id":"owner","permissions":["customers:create","products:create","products:manage-stock","orders:create","orders:cancel"]}""");
        var cancelCustomer = await CreateCustomer(client, "cancel@example.com");
        var cancelProduct = await CreateProduct(client, "CAN123");
        await client.PostAsJsonAsync($"/api/products/{cancelProduct}/stock-additions{Version}", new AddStockRequest(5));
        var cancelOrder = await PostJson(client, "/api/orders" + Version, new CreateOrderRequest(cancelCustomer, [new OrderLineItemRequest(cancelProduct, 1)]));
        client.DefaultRequestHeaders.Remove("X-Test-Actor");
        client.DefaultRequestHeaders.Add("X-Test-Actor", """{"id":"other","permissions":["orders:cancel"]}""");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync($"/api/orders/{cancelOrder["id"]!.GetValue<string>()}/cancellation{Version}", null)).StatusCode);
        client.DefaultRequestHeaders.Remove("X-Test-Actor");
        client.DefaultRequestHeaders.Add("X-Test-Actor", """{"id":"owner","permissions":["orders:cancel"]}""");
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/orders/{cancelOrder["id"]!.GetValue<string>()}/cancellation{Version}", null)).StatusCode);

        client.DefaultRequestHeaders.Remove("X-Test-Actor");
        var overdueOrder = await PostJson(client, "/api/orders" + Version, new CreateOrderRequest(customerId, [new OrderLineItemRequest(productId, 1)]));
        await client.PostAsync($"/api/orders/{overdueOrder["id"]!.GetValue<string>()}/submission{Version}", null);
        factory.Time.Advance(TimeSpan.FromDays(8));
        var overdue = (await JsonNode.ParseAsync(await (await client.GetAsync("/api/orders/overdue" + Version)).Content.ReadAsStreamAsync()))!.AsArray();
        Assert.Contains(overdue, item => item!["id"]!.GetValue<string>() == overdueOrder["id"]!.GetValue<string>());
    }

    private static CreateCustomerRequest MakeCustomer(string email) => new("Ada", "Lovelace", email, null, new ShippingAddress("1 Main", "Austin", "TX", "78701", "US"));
    private static async Task<Guid> CreateCustomer(HttpClient c, string email) => Guid.Parse((await PostJson(c, "/api/customers" + Version, MakeCustomer(email)))["id"]!.GetValue<string>());
    private static async Task<Guid> CreateProduct(HttpClient c, string sku) => Guid.Parse((await PostJson(c, "/api/products" + Version, new CreateProductRequest("Keyboard", sku, 10m)))["id"]!.GetValue<string>());
    private static async Task<JsonNode> PostJson(HttpClient c, string uri, object body) { var r = await c.PostAsJsonAsync(uri, body); r.EnsureSuccessStatusCode(); return (await JsonNode.ParseAsync(await r.Content.ReadAsStreamAsync()))!; }
}

public sealed class MutableTimeProvider(DateTimeOffset? initial = null) : TimeProvider
{
    private DateTimeOffset _utcNow = initial ?? new DateTimeOffset(2026, 06, 01, 0, 0, 0, TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow() => _utcNow;
    public void Advance(TimeSpan value) => _utcNow = _utcNow.Add(value);
}

file sealed class TestActorProvider(Actor actor) : IActorProvider
{
    public Actor Actor { get; set; } = actor;
    public Actor GetActor() => Actor;
}

file sealed class ServiceFixture : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly OrderManagementDbContext _db;
    public OrderManagementService Service { get; }
    public TestActorProvider ActorProvider { get; }
    private ServiceFixture(SqliteConnection connection, OrderManagementDbContext db, TestActorProvider actorProvider, TimeProvider timeProvider)
    {
        _connection = connection; _db = db; ActorProvider = actorProvider; Service = new OrderManagementService(db, actorProvider, timeProvider);
    }
    public static async Task<ServiceFixture> CreateAsync(Actor actor, TimeProvider timeProvider)
    {
        var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var db = new OrderManagementDbContext(new DbContextOptionsBuilder<OrderManagementDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var actorProvider = new TestActorProvider(actor);
        return new ServiceFixture(connection, db, actorProvider, timeProvider);
    }
    public async ValueTask DisposeAsync() { await _db.DisposeAsync(); await _connection.DisposeAsync(); }
}

file sealed class TestAppFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    public MutableTimeProvider Time { get; } = new(new DateTimeOffset(2026, 06, 01, 0, 0, 0, TimeSpan.Zero));
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<OrderManagementDbContext>>();
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Time);
            services.AddDbContext<OrderManagementDbContext>(options => options.UseSqlite(_connection));
        });
    }
    protected override void Dispose(bool disposing) { base.Dispose(disposing); _connection.Dispose(); }
}
