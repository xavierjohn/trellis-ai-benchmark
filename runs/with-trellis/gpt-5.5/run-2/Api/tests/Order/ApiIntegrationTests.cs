namespace Api.Tests.Order;

using System.Net;
using System.Net.Http.Json;
using MartinCostello.Logging.XUnit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrderManagement.AntiCorruptionLayer;
using Trellis.EntityFrameworkCore;
using Trellis.Testing.AspNetCore;
using Xunit.v3;

[Collection(WebAppCollection.Id)]
public sealed class ApiIntegrationTests
{
    private readonly HttpClient _client;

    public ApiIntegrationTests(WebAppFixture fixture, ITestOutputHelper output)
    {
        fixture.OutputHelper = output;
        _client = fixture.CreateClient();
    }

    [Fact]
    public async Task Health_returns_ok()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Missing_api_version_returns_bad_request()
    {
        var response = await _client.GetAsync("/api/orders/overdue");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Duplicate_email_returns_conflict_problem_details()
    {
        var request = CustomerRequest("dup@example.com");
        var first = await _client.PostAsJsonAsync("/api/customers?api-version=2026-11-12", request);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var duplicate = await _client.PostAsJsonAsync("/api/customers?api-version=2026-11-12", request);
        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await duplicate.Content.ReadFromJsonAsync<Dictionary<string, object>>()).Should().ContainKey("title");
    }

    [Fact]
    public async Task Missing_permission_returns_forbidden()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/products?api-version=2026-11-12")
        {
            Content = JsonContent.Create(new { productName = "Widget", sku = "WID123", unitPrice = 10m }),
        };
        request.Headers.Add("X-Test-Actor", """{"id":"actor-1","permissions":["orders:read"]}""");

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Full_order_lifecycle_succeeds()
    {
        var customerId = await CreateCustomerAsync("life@example.com");
        var productId = await CreateProductAsync("LIFE123");
        await PostOkAsync($"/api/products/{productId}/stock-additions?api-version=2026-11-12", new { quantity = 10 });

        var order = await PostCreatedAsync<OrderDto>("/api/orders?api-version=2026-11-12", new
        {
            customerId,
            lineItems = new[] { new { productId, quantity = 2 } },
        });

        order.Status.Should().Be("Draft");
        order = await PostOkAsync<OrderDto>($"/api/orders/{order.Id}/submission?api-version=2026-11-12");
        order.Status.Should().Be("Submitted");
        order = await PostOkAsync<OrderDto>($"/api/orders/{order.Id}/approval?api-version=2026-11-12");
        order.Status.Should().Be("Approved");
        order = await PostOkAsync<OrderDto>($"/api/orders/{order.Id}/shipment?api-version=2026-11-12");
        order.Status.Should().Be("Shipped");
        order = await PostOkAsync<OrderDto>($"/api/orders/{order.Id}/delivery?api-version=2026-11-12");
        order.Status.Should().Be("Delivered");
    }

    [Fact]
    public async Task Cancel_by_non_owner_is_forbidden_and_owner_succeeds()
    {
        var customerId = await CreateCustomerAsync("cancel@example.com", SalesActor);
        var productId = await CreateProductAsync("CAN123");
        await PostOkAsync($"/api/products/{productId}/stock-additions?api-version=2026-11-12", new { quantity = 10 });
        var order = await PostCreatedAsync<OrderDto>("/api/orders?api-version=2026-11-12", new
        {
            customerId,
            lineItems = new[] { new { productId, quantity = 2 } },
        }, SalesActor);

        var forbidden = await SendAsync(HttpMethod.Post, $"/api/orders/{order.Id}/cancellation?api-version=2026-11-12", null, OtherSalesActor);
        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var cancelled = await PostOkAsync<OrderDto>($"/api/orders/{order.Id}/cancellation?api-version=2026-11-12", null, SalesActor);
        cancelled.Status.Should().Be("Cancelled");
    }

    private async Task<Guid> CreateCustomerAsync(string email, string? actor = null) =>
        (await PostCreatedAsync<CustomerDto>("/api/customers?api-version=2026-11-12", CustomerRequest(email), actor)).Id;

    private async Task<Guid> CreateProductAsync(string sku) =>
        (await PostCreatedAsync<ProductDto>("/api/products?api-version=2026-11-12", new { productName = $"Product {sku}", sku, unitPrice = 10m })).Id;

    private async Task<T> PostCreatedAsync<T>(string url, object body, string? actor = null)
    {
        var response = await SendAsync(HttpMethod.Post, url, body, actor);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private async Task PostOkAsync(string url, object? body = null, string? actor = null)
    {
        var response = await SendAsync(HttpMethod.Post, url, body, actor);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<T> PostOkAsync<T>(string url, object? body = null, string? actor = null)
    {
        var response = await SendAsync(HttpMethod.Post, url, body, actor);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, object? body, string? actor = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (body is not null)
            request.Content = JsonContent.Create(body);
        if (actor is not null)
            request.Headers.Add("X-Test-Actor", actor);
        return _client.SendAsync(request);
    }

    private static object CustomerRequest(string email) => new
    {
        firstName = "Ada",
        lastName = "Lovelace",
        email,
        phoneNumber = "+15551234567",
        shippingAddress = new { street = "1 Main", city = "Seattle", state = "WA", postalCode = "98101", country = "USA" },
    };

    private const string SalesActor = """{"id":"sales-1","permissions":["customers:create","orders:create","orders:submit","orders:cancel","orders:read"]}""";
    private const string OtherSalesActor = """{"id":"sales-2","permissions":["orders:cancel","orders:read"]}""";

    private sealed record CustomerDto(Guid Id);
    private sealed record ProductDto(Guid Id);
    private sealed record OrderDto(Guid Id, string Status);
}

public sealed class WebAppFixture : WebApplicationFactory<Program>, ITestOutputHelperAccessor
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public WebAppFixture()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        _connection.Open();
    }

    public ITestOutputHelper? OutputHelper { get; set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging => logging.AddXUnit(this));
        builder.ConfigureServices(services => services.ReplaceDbProvider<AppDbContext>(options =>
            options.UseSqlite(_connection).AddTrellisInterceptors()));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }
}

[CollectionDefinition(Id)]
public sealed class WebAppCollection : ICollectionFixture<WebAppFixture>
{
    public const string Id = "Order API fixture";
}
