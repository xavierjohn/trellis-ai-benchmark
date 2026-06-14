using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace OrderManagement.Tests;

public sealed class DomainTests
{
    [Fact]
    public void Customer_creation_validates_required_values()
    {
        var address = TestData.Address();

        var customer = Customer.Create("Ada", "Lovelace", "ada@example.com", "+1 555 0100", address);

        Assert.Equal("ada@example.com", customer.Email);
        Assert.Throws<DomainValidationException>(() => Customer.Create("", "Lovelace", "ada@example.com", null, address));
        Assert.Throws<DomainValidationException>(() => Customer.Create("Ada", "Lovelace", "not-email", null, address));
        Assert.Throws<DomainValidationException>(() => Customer.Create("Ada", "Lovelace", "ada@example.com", "x", address));
    }

    [Fact]
    public void Product_stock_rules_are_enforced()
    {
        var product = Product.Create("Widget", "ABC123", 12.50m);

        product.AddStock(5);
        product.ReserveStock(3);

        Assert.Equal(2, product.StockQuantity);
        Assert.Throws<DomainValidationException>(() => product.AddStock(0));
        Assert.Throws<DomainValidationException>(() => product.ReserveStock(3));
        Assert.Throws<DomainValidationException>(() => Product.Create("Widget", "bad", 12.50m));
        Assert.Throws<DomainValidationException>(() => Product.Create("Widget", "ABC999", 0));
    }

    [Fact]
    public void Order_line_item_rules_are_enforced()
    {
        var product = Product.Create("Widget", "ABC123", 10m);
        var second = Product.Create("Gadget", "DEF456", 5m);
        var order = TestData.OrderWith(product);

        order.AddLineItem(second, 2);

        Assert.Equal(2, order.LineItems.Count);
        Assert.Equal(30m, order.Total);
        Assert.Throws<DomainValidationException>(() => order.AddLineItem(second, 1));
        Assert.Throws<DomainValidationException>(() => order.AddLineItem(Product.Create("Bad", "BAD999", 1), 1000));

        order.RemoveLineItem(order.LineItems.First(i => i.ProductId == second.Id).Id);
        Assert.Throws<DomainValidationException>(() => order.RemoveLineItem(order.LineItems.Single().Id));
    }

    [Fact]
    public void State_machine_reserves_and_releases_stock()
    {
        var product = Product.Create("Widget", "ABC123", 10m);
        product.AddStock(10);
        var order = TestData.OrderWith(product);
        var products = new Dictionary<Guid, Product> { [product.Id] = product };

        order.Submit(products, DateTimeOffset.UtcNow);
        Assert.Equal(OrderStatus.Submitted, order.Status);
        Assert.Equal(8, product.StockQuantity);

        order.Approve(DateTimeOffset.UtcNow);
        Assert.Equal(OrderStatus.Approved, order.Status);

        order.Cancel(products, DateTimeOffset.UtcNow);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(10, product.StockQuantity);
        Assert.Throws<DomainValidationException>(() => order.Ship(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Every_valid_lifecycle_transition_succeeds_and_invalid_transitions_fail()
    {
        var product = Product.Create("Widget", "ABC123", 10m);
        product.AddStock(10);
        var order = TestData.OrderWith(product);
        var products = new Dictionary<Guid, Product> { [product.Id] = product };

        Assert.Throws<DomainValidationException>(() => order.Approve(DateTimeOffset.UtcNow));
        order.Submit(products, DateTimeOffset.UtcNow);
        order.Approve(DateTimeOffset.UtcNow);
        order.Ship(DateTimeOffset.UtcNow);
        order.Deliver(DateTimeOffset.UtcNow);

        Assert.Equal(OrderStatus.Delivered, order.Status);
        Assert.Throws<DomainValidationException>(() => order.Cancel(products, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Overdue_specification_matches_only_old_submitted_orders()
    {
        var product = Product.Create("Widget", "ABC123", 10m);
        product.AddStock(20);
        var now = new DateTimeOffset(2026, 6, 13, 0, 0, 0, TimeSpan.Zero);
        var oldSubmitted = TestData.OrderWith(product);
        var recentSubmitted = TestData.OrderWith(product);
        var approved = TestData.OrderWith(product);
        var products = new Dictionary<Guid, Product> { [product.Id] = product };

        oldSubmitted.Submit(products, now.AddDays(-8));
        recentSubmitted.Submit(products, now.AddDays(-2));
        approved.Submit(products, now.AddDays(-8));
        approved.Approve(now.AddDays(-7));

        Assert.True(oldSubmitted.IsOverdue(now));
        Assert.False(recentSubmitted.IsOverdue(now));
        Assert.False(approved.IsOverdue(now));
    }
}

public sealed class ApplicationTests
{
    [Fact]
    public async Task Authorization_allows_correct_permission_and_rejects_missing_permission()
    {
        await using var fixture = await ServiceFixture.CreateAsync();
        var service = fixture.Services.GetRequiredService<OrderService>();

        var customer = await service.CreateCustomerAsync(TestData.CreateCustomerRequest("app-auth@example.com"), TestData.AdminActor);

        Assert.NotEqual(Guid.Empty, customer.Id);
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.CreateCustomerAsync(TestData.CreateCustomerRequest("denied@example.com"), new Actor("x", new HashSet<string>())));
    }

    [Fact]
    public async Task Cancel_resource_authorization_enforces_owner_or_admin()
    {
        await using var fixture = await ServiceFixture.CreateAsync();
        var service = fixture.Services.GetRequiredService<OrderService>();
        var owner = new Actor("owner", new HashSet<string> { Permissions.CustomersCreate, Permissions.ProductsCreate, Permissions.OrdersCreate, Permissions.OrdersCancel });
        var other = new Actor("other", new HashSet<string> { Permissions.OrdersCancel });
        var admin = TestData.AdminActor;

        var customer = await service.CreateCustomerAsync(TestData.CreateCustomerRequest("owner@example.com"), owner);
        var product = await service.CreateProductAsync(new CreateProductRequest("Widget", "OWN123", 10m), admin);
        var ownerOrder = await service.CreateOrderAsync(new CreateOrderRequest(customer.Id, [new(product.Id, 1)]), owner);
        var adminOrder = await service.CreateOrderAsync(new CreateOrderRequest(customer.Id, [new(product.Id, 1)]), owner);

        await Assert.ThrowsAsync<ForbiddenException>(() => service.CancelOrderAsync(ownerOrder.Id, other));

        var cancelledByOwner = await service.CancelOrderAsync(ownerOrder.Id, owner);
        var cancelledByAdmin = await service.CancelOrderAsync(adminOrder.Id, admin);

        Assert.Equal(OrderStatus.Cancelled, cancelledByOwner.Status);
        Assert.Equal(OrderStatus.Cancelled, cancelledByAdmin.Status);
    }

    [Fact]
    public async Task Not_found_errors_are_returned_when_entities_do_not_exist()
    {
        await using var fixture = await ServiceFixture.CreateAsync();
        var service = fixture.Services.GetRequiredService<OrderService>();

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetOrderAsync(Guid.NewGuid(), TestData.AdminActor));
    }
}

public sealed class ApiIntegrationTests
{
    [Fact]
    public async Task Create_customer_returns_created_with_location()
    {
        await using var factory = new TestOrderApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(Versioned("/api/customers"), TestData.CreateCustomerRequest("api-create@example.com"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task Duplicate_email_returns_conflict_problem_details()
    {
        await using var factory = new TestOrderApplicationFactory();
        var client = factory.CreateClient();

        await client.PostAsJsonAsync(Versioned("/api/customers"), TestData.CreateCustomerRequest("duplicate@example.com"));
        var duplicate = await client.PostAsJsonAsync(Versioned("/api/customers"), TestData.CreateCustomerRequest("duplicate@example.com"));
        var problem = await duplicate.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("Conflict", problem.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Missing_permission_returns_forbidden()
    {
        await using var factory = new TestOrderApplicationFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Actor", JsonSerializer.Serialize(new { id = "limited", permissions = Array.Empty<string>() }));

        var response = await client.PostAsJsonAsync(Versioned("/api/customers"), TestData.CreateCustomerRequest("forbidden@example.com"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Full_order_lifecycle_succeeds()
    {
        await using var factory = new TestOrderApplicationFactory();
        var client = factory.CreateClient();

        var customer = await CreateCustomerAsync(client, "lifecycle@example.com");
        var product = await CreateProductAsync(client, "LIFE123");
        await client.PostAsJsonAsync(Versioned($"/api/products/{product.Id}/stock-additions"), new AddStockRequest(5));

        var order = await CreateOrderAsync(client, customer.Id, product.Id, 2);
        order = await PostOrderTransitionAsync(client, order.Id, "submission");
        order = await PostOrderTransitionAsync(client, order.Id, "approval");
        order = await PostOrderTransitionAsync(client, order.Id, "shipment");
        order = await PostOrderTransitionAsync(client, order.Id, "delivery");

        Assert.Equal(OrderStatus.Delivered, order.Status);
    }

    [Fact]
    public async Task Cancel_by_non_owner_is_forbidden_and_owner_succeeds()
    {
        await using var factory = new TestOrderApplicationFactory();
        var client = factory.CreateClient();
        var sales = new { id = "sales-1", permissions = new[] { Permissions.CustomersCreate, Permissions.OrdersCreate, Permissions.OrdersCancel } };
        client.DefaultRequestHeaders.Add("X-Test-Actor", JsonSerializer.Serialize(sales));

        var customer = await CreateCustomerAsync(client, "cancel@example.com");
        client.DefaultRequestHeaders.Remove("X-Test-Actor");
        var product = await CreateProductAsync(client, "CANCEL123");
        client.DefaultRequestHeaders.Add("X-Test-Actor", JsonSerializer.Serialize(sales));
        var order = await CreateOrderAsync(client, customer.Id, product.Id, 1);

        client.DefaultRequestHeaders.Remove("X-Test-Actor");
        client.DefaultRequestHeaders.Add("X-Test-Actor", JsonSerializer.Serialize(new { id = "other", permissions = new[] { Permissions.OrdersCancel } }));
        var forbidden = await client.PostAsync(Versioned($"/api/orders/{order.Id}/cancellation"), null);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        client.DefaultRequestHeaders.Remove("X-Test-Actor");
        client.DefaultRequestHeaders.Add("X-Test-Actor", JsonSerializer.Serialize(sales));
        var cancelled = await PostOrderTransitionAsync(client, order.Id, "cancellation");
        Assert.Equal(OrderStatus.Cancelled, cancelled.Status);
    }

    [Fact]
    public async Task Overdue_orders_query_filters_submitted_orders_older_than_seven_days()
    {
        var time = new MutableTimeProvider(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));
        await using var factory = new TestOrderApplicationFactory(time);
        var client = factory.CreateClient();

        var customer = await CreateCustomerAsync(client, "overdue@example.com");
        var oldProduct = await CreateProductAsync(client, "OLD123");
        var recentProduct = await CreateProductAsync(client, "RECENT123");
        await client.PostAsJsonAsync(Versioned($"/api/products/{oldProduct.Id}/stock-additions"), new AddStockRequest(5));
        await client.PostAsJsonAsync(Versioned($"/api/products/{recentProduct.Id}/stock-additions"), new AddStockRequest(5));
        var oldOrder = await CreateOrderAsync(client, customer.Id, oldProduct.Id, 1);
        var recentOrder = await CreateOrderAsync(client, customer.Id, recentProduct.Id, 1);
        await PostOrderTransitionAsync(client, oldOrder.Id, "submission");
        time.UtcNow = time.UtcNow.AddDays(8);
        await PostOrderTransitionAsync(client, recentOrder.Id, "submission");

        var response = await client.GetFromJsonAsync<List<OrderResponse>>(Versioned("/api/orders/overdue"));

        Assert.Single(response!);
        Assert.Equal(oldOrder.Id, response![0].Id);
    }

    [Fact]
    public async Task Missing_api_version_and_health_behave_as_specified()
    {
        await using var factory = new TestOrderApplicationFactory();
        var client = factory.CreateClient();

        var missingVersion = await client.PostAsJsonAsync("/api/customers", TestData.CreateCustomerRequest("missing-version@example.com"));
        var health = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.BadRequest, missingVersion.StatusCode);
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
    }

    private static string Versioned(string path) => $"{path}?api-version={ApiVersion.Value}";

    private static async Task<CustomerResponse> CreateCustomerAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync(Versioned("/api/customers"), TestData.CreateCustomerRequest(email));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CustomerResponse>())!;
    }

    private static async Task<ProductResponse> CreateProductAsync(HttpClient client, string sku)
    {
        var response = await client.PostAsJsonAsync(Versioned("/api/products"), new CreateProductRequest("Widget", sku, 10m));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductResponse>())!;
    }

    private static async Task<OrderResponse> CreateOrderAsync(HttpClient client, Guid customerId, Guid productId, int quantity)
    {
        var response = await client.PostAsJsonAsync(Versioned("/api/orders"), new CreateOrderRequest(customerId, [new(productId, quantity)]));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OrderResponse>())!;
    }

    private static async Task<OrderResponse> PostOrderTransitionAsync(HttpClient client, Guid orderId, string transition)
    {
        var response = await client.PostAsync(Versioned($"/api/orders/{orderId}/{transition}"), new StringContent("", Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OrderResponse>())!;
    }
}

public sealed class TestOrderApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;
    private readonly TimeProvider _timeProvider;

    public TestOrderApplicationFactory(TimeProvider? timeProvider = null)
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _timeProvider = timeProvider ?? new MutableTimeProvider(DateTimeOffset.UtcNow);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<OrderManagementDbContext>>();
            services.RemoveAll<TimeProvider>();
            services.AddSingleton(_timeProvider);
            services.AddDbContext<OrderManagementDbContext>(options => options.UseSqlite(_connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection.Dispose();
        }

        base.Dispose(disposing);
    }
}

public sealed class ServiceFixture : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;

    private ServiceFixture(SqliteConnection connection, ServiceProvider provider)
    {
        _connection = connection;
        _provider = provider;
    }

    public IServiceProvider Services => _provider;

    public static async Task<ServiceFixture> CreateAsync(TimeProvider? timeProvider = null)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(timeProvider ?? new MutableTimeProvider(DateTimeOffset.UtcNow));
        services.AddDbContext<OrderManagementDbContext>(options => options.UseSqlite(connection));
        services.AddScoped<OrderService>();
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<OrderManagementDbContext>().Database.EnsureCreatedAsync();
        return new ServiceFixture(connection, provider);
    }

    public async ValueTask DisposeAsync()
    {
        await _provider.DisposeAsync();
        await _connection.DisposeAsync();
    }
}

public sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public DateTimeOffset UtcNow { get; set; } = utcNow;
    public override DateTimeOffset GetUtcNow() => UtcNow;
}

internal static class TestData
{
    public static readonly Actor AdminActor = new("admin", Permissions.All.ToHashSet(StringComparer.Ordinal));

    public static ShippingAddress Address() => new("1 Main St", "Seattle", "WA", "98101", "US");

    public static CreateCustomerRequest CreateCustomerRequest(string email) =>
        new("Ada", "Lovelace", email, null, Address());

    public static Order OrderWith(Product product) =>
        Order.Create(Guid.NewGuid(), "actor-1", [LineItem.Create(product.Id, product.ProductName, 2, product.UnitPrice)], DateTimeOffset.UtcNow);
}
