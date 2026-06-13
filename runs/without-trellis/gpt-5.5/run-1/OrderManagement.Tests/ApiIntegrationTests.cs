using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OrderManagement.Api;

namespace OrderManagement.Tests;

public sealed class ApiIntegrationTests
{
    [Fact]
    public async Task Create_customer_returns_created_with_location()
    {
        await using var factory = new TestApiFactory();
        var response = await factory.CreateClient().PostAsJsonAsync(Versioned("/api/customers"), Customer("ada@example.com"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task Duplicate_email_returns_conflict_problem_details()
    {
        await using var factory = new TestApiFactory();
        var client = factory.CreateClient();

        await client.PostAsJsonAsync(Versioned("/api/customers"), Customer("ada@example.com"));
        var duplicate = await client.PostAsJsonAsync(Versioned("/api/customers"), Customer("ada@example.com"));

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var problem = await duplicate.Content.ReadFromJsonAsync<ProblemShape>();
        Assert.Equal(409, problem?.Status);
    }

    [Fact]
    public async Task Missing_permission_returns_forbidden()
    {
        await using var factory = new TestApiFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Actor", """{"id":"actor-1","permissions":[]}""");

        var response = await client.PostAsJsonAsync(Versioned("/api/customers"), Customer("ada@example.com"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Full_order_lifecycle_succeeds()
    {
        await using var factory = new TestApiFactory();
        var client = factory.CreateClient();
        var customer = await CreateCustomer(client, "ada@example.com");
        var product = await CreateProduct(client, "KEY123");
        await client.PostAsJsonAsync(Versioned($"/api/products/{product.Id}/stock-additions"), new AddStockRequest(10));

        var order = await CreateOrder(client, customer.Id, product.Id, 2);
        order = await PostOrderAction(client, order.Id, "submission");
        order = await PostOrderAction(client, order.Id, "approval");
        order = await PostOrderAction(client, order.Id, "shipment");
        order = await PostOrderAction(client, order.Id, "delivery");

        Assert.Equal("Delivered", order.Status);
    }

    [Fact]
    public async Task Cancel_by_non_owner_is_forbidden_and_owner_can_cancel()
    {
        await using var factory = new TestApiFactory();
        var client = factory.CreateClient();
        var customer = await CreateCustomer(client, "ada@example.com");
        var product = await CreateProduct(client, "KEY123");
        await client.PostAsJsonAsync(Versioned($"/api/products/{product.Id}/stock-additions"), new AddStockRequest(10));

        client.DefaultRequestHeaders.Remove("X-Test-Actor");
        client.DefaultRequestHeaders.Add("X-Test-Actor", Actor("owner", Permissions.OrdersCreate, Permissions.OrdersCancel));
        var order = await CreateOrder(client, customer.Id, product.Id, 1);

        client.DefaultRequestHeaders.Remove("X-Test-Actor");
        client.DefaultRequestHeaders.Add("X-Test-Actor", Actor("other", Permissions.OrdersCancel));
        var forbidden = await client.PostAsync(Versioned($"/api/orders/{order.Id}/cancellation"), null);

        client.DefaultRequestHeaders.Remove("X-Test-Actor");
        client.DefaultRequestHeaders.Add("X-Test-Actor", Actor("owner", Permissions.OrdersCancel));
        var ownerResponse = await client.PostAsync(Versioned($"/api/orders/{order.Id}/cancellation"), null);

        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ownerResponse.StatusCode);
    }

    [Fact]
    public async Task Overdue_orders_query_filters_old_submitted_orders()
    {
        await using var factory = new TestApiFactory();
        var client = factory.CreateClient();
        var oldCustomer = await CreateCustomer(client, "old@example.com");
        var recentCustomer = await CreateCustomer(client, "recent@example.com");
        var product = await CreateProduct(client, "KEY123");
        await client.PostAsJsonAsync(Versioned($"/api/products/{product.Id}/stock-additions"), new AddStockRequest(10));

        factory.Time.SetUtcNow(new DateTimeOffset(2026, 11, 1, 12, 0, 0, TimeSpan.Zero));
        var old = await CreateOrder(client, oldCustomer.Id, product.Id, 1);
        await PostOrderAction(client, old.Id, "submission");

        factory.Time.SetUtcNow(new DateTimeOffset(2026, 11, 11, 12, 0, 0, TimeSpan.Zero));
        var recent = await CreateOrder(client, recentCustomer.Id, product.Id, 1);
        await PostOrderAction(client, recent.Id, "submission");

        factory.Time.SetUtcNow(new DateTimeOffset(2026, 11, 12, 12, 0, 0, TimeSpan.Zero));
        var response = await client.GetFromJsonAsync<OrderDto[]>(Versioned("/api/orders/overdue"));

        Assert.NotNull(response);
        Assert.Contains(response, o => o.Id == old.Id);
        Assert.DoesNotContain(response, o => o.Id == recent.Id);
    }

    [Fact]
    public async Task Missing_api_version_returns_bad_request()
    {
        await using var factory = new TestApiFactory();
        var response = await factory.CreateClient().GetAsync("/api/orders/overdue");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Health_check_returns_ok()
    {
        await using var factory = new TestApiFactory();
        var response = await factory.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task<CustomerDto> CreateCustomer(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync(Versioned("/api/customers"), Customer(email));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CustomerDto>())!;
    }

    private static async Task<ProductDto> CreateProduct(HttpClient client, string sku)
    {
        var response = await client.PostAsJsonAsync(Versioned("/api/products"), new CreateProductRequest("Keyboard", sku, 10m));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductDto>())!;
    }

    private static async Task<OrderDto> CreateOrder(HttpClient client, Guid customerId, Guid productId, int quantity)
    {
        var response = await client.PostAsJsonAsync(Versioned("/api/orders"), new CreateOrderRequest(customerId, [new CreateOrderLineItemRequest(productId, quantity)]));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OrderDto>())!;
    }

    private static async Task<OrderDto> PostOrderAction(HttpClient client, Guid orderId, string action)
    {
        var response = await client.PostAsync(Versioned($"/api/orders/{orderId}/{action}"), null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OrderDto>())!;
    }

    private static CreateCustomerRequest Customer(string email) => new("Ada", "Lovelace", email, "+1 555 123 4567", new AddressDto("1 Main", "Seattle", "WA", "98101", "USA"));
    private static string Versioned(string path) => $"{path}?api-version=2026-11-12";
    private static string Actor(string id, params string[] permissions) => $$"""{"id":"{{id}}","permissions":[{{string.Join(',', permissions.Select(p => $"\"{p}\""))}}]}""";
}

public sealed class TestApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 11, 12, 12, 0, 0, TimeSpan.Zero));

    public TestApiFactory() => _connection.Open();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<TimeProvider>();
            services.AddSingleton(_connection);
            services.AddSingleton<TimeProvider>(Time);
            services.AddDbContext<AppDbContext>((sp, options) => options.UseSqlite(sp.GetRequiredService<SqliteConnection>()));
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
        await base.DisposeAsync();
    }
}

public sealed record CustomerDto(Guid Id, string Email);
public sealed record ProductDto(Guid Id, string Sku, int StockQuantity);
public sealed record OrderDto(Guid Id, string Status);
public sealed record ProblemShape(int? Status, string? Title, string? Detail);
