using Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Api.Tests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly SqliteConnection _connection = new($"Data Source=test-{Guid.NewGuid()};Mode=Memory;Cache=Shared");

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    public new async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.RemoveAll(typeof(AppDbContext));

            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
        });
    }
}

public class ApiTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _client = factory.CreateClient();

    private const string V = "?api-version=2026-11-12";
    private static string AdminActor => JsonSerializer.Serialize(new
    {
        id = "admin",
        permissions = new[]
        {
            "customers:create", "products:create", "products:manage-stock",
            "orders:create", "orders:submit", "orders:approve", "orders:ship",
            "orders:deliver", "orders:cancel", "orders:read", "orders:read-all"
        }
    });
    private static string NoPerms => JsonSerializer.Serialize(new { id = "noperms", permissions = Array.Empty<string>() });

    private static HttpRequestMessage WithActor(HttpMethod method, string url, string actor, object? body = null)
    {
        var msg = new HttpRequestMessage(method, url);
        msg.Headers.Add("X-Test-Actor", actor);
        if (body != null)
        {
            msg.Content = JsonContent.Create(body);
        }

        return msg;
    }

    [Fact]
    public async Task CreateCustomer_Returns201WithLocation()
    {
        var req = WithActor(HttpMethod.Post, $"/api/customers{V}", AdminActor, new
        {
            firstName = "John",
            lastName = "Doe",
            email = $"john{Guid.NewGuid()}@example.com",
            street = "123 Main St",
            city = "Springfield",
            state = "IL",
            postalCode = "62701",
            country = "US"
        });

        var resp = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        Assert.NotNull(resp.Headers.Location);
    }

    [Fact]
    public async Task CreateCustomer_DuplicateEmail_Returns409()
    {
        var email = $"dup{Guid.NewGuid()}@example.com";
        var body = new
        {
            firstName = "John",
            lastName = "Doe",
            email,
            street = "123 Main St",
            city = "City",
            state = "ST",
            postalCode = "12345",
            country = "US"
        };

        await _client.SendAsync(WithActor(HttpMethod.Post, $"/api/customers{V}", AdminActor, body));
        var resp = await _client.SendAsync(WithActor(HttpMethod.Post, $"/api/customers{V}", AdminActor, body));
        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    [Fact]
    public async Task CreateCustomer_MissingPermission_Returns403()
    {
        var req = WithActor(HttpMethod.Post, $"/api/customers{V}", NoPerms, new
        {
            firstName = "John",
            lastName = "Doe",
            email = $"test{Guid.NewGuid()}@example.com",
            street = "123 St",
            city = "City",
            state = "ST",
            postalCode = "12345",
            country = "US"
        });

        var resp = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task MissingApiVersion_Returns400()
    {
        var resp = await _client.GetAsync($"/api/orders/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task HealthCheck_Returns200()
    {
        var resp = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task FullOrderLifecycle_Succeeds()
    {
        var custResp = await _client.SendAsync(WithActor(HttpMethod.Post, $"/api/customers{V}", AdminActor, new
        {
            firstName = "Alice",
            lastName = "Smith",
            email = $"lifecycle{Guid.NewGuid()}@example.com",
            street = "1 Main St",
            city = "City",
            state = "ST",
            postalCode = "11111",
            country = "US"
        }));
        Assert.Equal(HttpStatusCode.Created, custResp.StatusCode);
        var custBody = await custResp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var custId = custBody.GetProperty("customerId").GetGuid();

        var sku = $"P{Guid.NewGuid():N}".ToUpperInvariant()[..9];
        var prodResp = await _client.SendAsync(WithActor(HttpMethod.Post, $"/api/products{V}", AdminActor, new
        {
            productName = "Test Product",
            sku,
            unitPrice = 10.00m,
            stockQuantity = 0
        }));
        Assert.Equal(HttpStatusCode.Created, prodResp.StatusCode);
        var prodBody = await prodResp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var prodId = prodBody.GetProperty("productId").GetGuid();

        var stockResp = await _client.SendAsync(WithActor(HttpMethod.Post, $"/api/products/{prodId}/stock-additions{V}", AdminActor, new { quantity = 50 }));
        Assert.Equal(HttpStatusCode.OK, stockResp.StatusCode);

        var orderResp = await _client.SendAsync(WithActor(HttpMethod.Post, $"/api/orders{V}", AdminActor, new { customerId = custId }));
        Assert.Equal(HttpStatusCode.Created, orderResp.StatusCode);
        var orderBody = await orderResp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var orderId = orderBody.GetProperty("orderId").GetGuid();

        var lineItemResp = await _client.SendAsync(WithActor(HttpMethod.Post, $"/api/orders/{orderId}/line-items{V}", AdminActor, new { productId = prodId, quantity = 5 }));
        Assert.Equal(HttpStatusCode.OK, lineItemResp.StatusCode);

        var submitResp = await _client.SendAsync(WithActor(HttpMethod.Post, $"/api/orders/{orderId}/submission{V}", AdminActor));
        Assert.Equal(HttpStatusCode.OK, submitResp.StatusCode);

        var approveResp = await _client.SendAsync(WithActor(HttpMethod.Post, $"/api/orders/{orderId}/approval{V}", AdminActor));
        Assert.Equal(HttpStatusCode.OK, approveResp.StatusCode);

        var shipResp = await _client.SendAsync(WithActor(HttpMethod.Post, $"/api/orders/{orderId}/shipment{V}", AdminActor));
        Assert.Equal(HttpStatusCode.OK, shipResp.StatusCode);

        var deliverResp = await _client.SendAsync(WithActor(HttpMethod.Post, $"/api/orders/{orderId}/delivery{V}", AdminActor));
        Assert.Equal(HttpStatusCode.OK, deliverResp.StatusCode);

        var getResp = await _client.SendAsync(WithActor(HttpMethod.Get, $"/api/orders/{orderId}{V}", AdminActor));
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
        var finalOrder = await getResp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal("Delivered", finalOrder.GetProperty("status").GetString());
    }

    [Fact]
    public async Task CancelByNonOwner_Returns403()
    {
        var custResp = await _client.SendAsync(WithActor(HttpMethod.Post, $"/api/customers{V}", AdminActor, new
        {
            firstName = "Bob",
            lastName = "Jones",
            email = $"cancel{Guid.NewGuid()}@example.com",
            street = "2 St",
            city = "City",
            state = "ST",
            postalCode = "22222",
            country = "US"
        }));
        var custBody = await custResp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var custId = custBody.GetProperty("customerId").GetGuid();

        var ownerActor = JsonSerializer.Serialize(new { id = "owner-1", permissions = new[] { "orders:create", "orders:cancel", "orders:read" } });
        var orderResp = await _client.SendAsync(WithActor(HttpMethod.Post, $"/api/orders{V}", ownerActor, new { customerId = custId }));
        var orderBody = await orderResp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var orderId = orderBody.GetProperty("orderId").GetGuid();

        var otherActor = JsonSerializer.Serialize(new { id = "other-1", permissions = new[] { "orders:cancel" } });
        var cancelResp = await _client.SendAsync(WithActor(HttpMethod.Post, $"/api/orders/{orderId}/cancellation{V}", otherActor));
        Assert.Equal(HttpStatusCode.Forbidden, cancelResp.StatusCode);
    }

    [Fact]
    public async Task CancelByOwner_Returns200()
    {
        var custResp = await _client.SendAsync(WithActor(HttpMethod.Post, $"/api/customers{V}", AdminActor, new
        {
            firstName = "Carol",
            lastName = "Jones",
            email = $"cancelown{Guid.NewGuid()}@example.com",
            street = "3 St",
            city = "City",
            state = "ST",
            postalCode = "33333",
            country = "US"
        }));
        var custBody = await custResp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var custId = custBody.GetProperty("customerId").GetGuid();

        var ownerActor = JsonSerializer.Serialize(new { id = "owner-2", permissions = new[] { "orders:create", "orders:cancel", "orders:read" } });
        var orderResp = await _client.SendAsync(WithActor(HttpMethod.Post, $"/api/orders{V}", ownerActor, new { customerId = custId }));
        var orderBody = await orderResp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var orderId = orderBody.GetProperty("orderId").GetGuid();

        var cancelResp = await _client.SendAsync(WithActor(HttpMethod.Post, $"/api/orders/{orderId}/cancellation{V}", ownerActor));
        Assert.Equal(HttpStatusCode.OK, cancelResp.StatusCode);
    }

    [Fact]
    public async Task OverdueOrders_Returns200WithCorrectList()
    {
        var resp = await _client.SendAsync(WithActor(HttpMethod.Get, $"/api/orders/overdue{V}", AdminActor));
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }
}
