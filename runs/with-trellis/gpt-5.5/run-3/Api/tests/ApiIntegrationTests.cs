namespace OrderManagement.Api.Tests;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using OrderManagement.Domain;

public class ApiIntegrationTests : IClassFixture<OrderManagementFactory>
{
    private readonly OrderManagementFactory _factory;

    public ApiIntegrationTests(OrderManagementFactory factory) => _factory = factory;

    [Fact]
    public async Task Health_check_returns_ok()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Missing_api_version_returns_bad_request()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/customers", ValidCustomer("noversion@example.com"));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_customer_duplicate_and_missing_permission_behave_as_specified()
    {
        var client = _factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@example.com";

        var created = await client.PostAsJsonAsync("/api/customers?api-version=2026-11-12", ValidCustomer(email));
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        created.Headers.Location.Should().NotBeNull();

        var duplicate = await client.PostAsJsonAsync("/api/customers?api-version=2026-11-12", ValidCustomer(email));
        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await duplicate.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetInt32().Should().Be(409);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/customers?api-version=2026-11-12")
        {
            Content = JsonContent.Create(ValidCustomer($"{Guid.NewGuid():N}@example.com")),
        };
        request.Headers.Add("X-Test-Actor", """{"id":"sales","permissions":["orders:create"]}""");
        var forbidden = await client.SendAsync(request);
        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Full_order_lifecycle_reaches_delivered()
    {
        var client = _factory.CreateClient();
        var customerId = await CreateCustomer(client);
        var productId = await CreateProduct(client);
        await AddStock(client, productId, 10);

        var order = await PostJson(client, "/api/orders?api-version=2026-11-12", new
        {
            customerId,
            lineItems = new[] { new { productId, quantity = 2 } },
        }, HttpStatusCode.Created);
        var orderId = order.GetProperty("id").GetGuid();

        foreach (var path in new[] { "submission", "approval", "shipment", "delivery" })
            order = await PostJson(client, $"/api/orders/{orderId}/{path}?api-version=2026-11-12", null, HttpStatusCode.OK);

        order.GetProperty("status").GetString().Should().Be("Delivered");
    }

    [Fact]
    public async Task Cancel_owner_rules_and_overdue_query_work()
    {
        var client = _factory.CreateClient();
        var customerId = await CreateCustomer(client);
        var productId = await CreateProduct(client);
        await AddStock(client, productId, 10);

        var order = await SendAsActor(client, HttpMethod.Post, "/api/orders?api-version=2026-11-12", new
        {
            customerId,
            lineItems = new[] { new { productId, quantity = 1 } },
        }, "owner", [Permissions.OrdersCreate], HttpStatusCode.Created);
        var orderId = order.GetProperty("id").GetGuid();

        await PostJson(client, $"/api/orders/{orderId}/submission?api-version=2026-11-12", null, HttpStatusCode.OK);
        _factory.Time.Advance(TimeSpan.FromDays(8));

        var overdue = await GetJson(client, "/api/orders/overdue?api-version=2026-11-12", HttpStatusCode.OK);
        overdue.GetArrayLength().Should().BeGreaterThan(0);

        await SendAsActor(client, HttpMethod.Post, $"/api/orders/{orderId}/cancellation?api-version=2026-11-12", null, "other", [Permissions.OrdersCancel], HttpStatusCode.Forbidden);
        var cancelled = await SendAsActor(client, HttpMethod.Post, $"/api/orders/{orderId}/cancellation?api-version=2026-11-12", null, "owner", [Permissions.OrdersCancel], HttpStatusCode.OK);
        cancelled.GetProperty("status").GetString().Should().Be("Cancelled");
    }

    private static object ValidCustomer(string email) => new
    {
        firstName = "Ada",
        lastName = "Lovelace",
        email,
        shippingAddress = new { street = "1 Main", city = "London", state = "LDN", postalCode = "SW1", country = "UK" },
    };

    private static async Task<Guid> CreateCustomer(HttpClient client)
    {
        var json = await PostJson(client, "/api/customers?api-version=2026-11-12", ValidCustomer($"{Guid.NewGuid():N}@example.com"), HttpStatusCode.Created);
        return json.GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateProduct(HttpClient client)
    {
        var json = await PostJson(client, "/api/products?api-version=2026-11-12", new
        {
            productName = "Widget",
            sku = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
            unitPrice = 10m,
        }, HttpStatusCode.Created);
        return json.GetProperty("id").GetGuid();
    }

    private static async Task AddStock(HttpClient client, Guid productId, int quantity) =>
        _ = await PostJson(client, $"/api/products/{productId}/stock-additions?api-version=2026-11-12", new { quantity }, HttpStatusCode.OK);

    private static async Task<JsonElement> GetJson(HttpClient client, string url, HttpStatusCode expected)
    {
        var response = await client.GetAsync(url);
        response.StatusCode.Should().Be(expected);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<JsonElement> PostJson(HttpClient client, string url, object? body, HttpStatusCode expected)
    {
        var response = body is null
            ? await client.PostAsync(url, null)
            : await client.PostAsJsonAsync(url, body);
        response.StatusCode.Should().Be(expected);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<JsonElement> SendAsActor(HttpClient client, HttpMethod method, string url, object? body, string id, string[] permissions, HttpStatusCode expected)
    {
        using var request = new HttpRequestMessage(method, url)
        {
            Content = body is null ? null : JsonContent.Create(body),
        };
        request.Headers.Add("X-Test-Actor", JsonSerializer.Serialize(new { id, permissions }));
        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(expected);
        return response.Content.Headers.ContentLength == 0
            ? default
            : await response.Content.ReadFromJsonAsync<JsonElement>();
    }
}

public sealed class OrderManagementFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"OrderManagement-{Guid.NewGuid():N}.db");

    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = $"Data Source={_databasePath}",
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Time);
        });
    }
}
