namespace Api.Tests;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

[Collection(TestWebApplicationFactoryCollectionFixture.Id)]
public sealed class OrderManagementApiTests
{
    private const string Version = "?api-version=2026-11-12";
    private readonly HttpClient _client;

    public OrderManagementApiTests(TestWebApplicationFactoryFixture factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task Create_customer_returns_location_and_duplicate_email_returns_conflict()
    {
        var email = UniqueEmail();
        var response = await CreateCustomer(email);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var duplicate = await CreateCustomer(email);
        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await duplicate.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken)).GetProperty("status").GetInt32().Should().Be(409);
    }

    [Fact]
    public async Task Missing_permission_returns_forbidden()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/products" + Version)
        {
            Content = JsonContent.Create(new { productName = "Widget", sku = UniqueSku(), unitPrice = 10m }),
        };
        request.Headers.Add("X-Test-Actor", """{"id":"limited","permissions":["orders:read"]}""");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Full_order_lifecycle_succeeds()
    {
        var customerId = await CreateCustomerId();
        var productId = await CreateProductWithStock(10);
        var orderId = await CreateOrder(customerId, productId, 2, SalesActor);

        (await Post($"/api/orders/{orderId}/submission{Version}", null, SalesActor)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await Post($"/api/orders/{orderId}/approval{Version}", null, WarehouseActor)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await Post($"/api/orders/{orderId}/shipment{Version}", null, WarehouseActor)).StatusCode.Should().Be(HttpStatusCode.OK);
        var delivered = await Post($"/api/orders/{orderId}/delivery{Version}", null, WarehouseActor);

        delivered.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await delivered.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        body.GetProperty("status").GetString().Should().Be("Delivered");
    }

    [Fact]
    public async Task Cancel_by_non_owner_is_forbidden_but_owner_succeeds()
    {
        var customerId = await CreateCustomerId();
        var productId = await CreateProductWithStock(10);
        var orderId = await CreateOrder(customerId, productId, 1, SalesActor);

        (await Post($"/api/orders/{orderId}/cancellation{Version}", null, OtherSalesActor)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Post($"/api/orders/{orderId}/cancellation{Version}", null, SalesActor)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Overdue_orders_query_returns_ok()
    {
        var response = await Get("/api/orders/overdue" + Version, WarehouseActor);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Missing_api_version_returns_bad_request()
    {
        var response = await _client.GetAsync("/api/orders/overdue", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Health_check_returns_ok()
    {
        var response = await _client.GetAsync("/health", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static string SalesActor => """{"id":"sales-1","permissions":["customers:create","orders:create","orders:submit","orders:cancel","orders:read"]}""";
    private static string OtherSalesActor => """{"id":"sales-2","permissions":["orders:cancel","orders:read"]}""";
    private static string WarehouseActor => """{"id":"warehouse-1","permissions":["products:create","products:manage-stock","orders:approve","orders:ship","orders:deliver","orders:read-all"]}""";

    private async Task<Guid> CreateCustomerId()
    {
        var response = await CreateCustomer(UniqueEmail());
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        return body.GetProperty("id").GetGuid();
    }

    private Task<HttpResponseMessage> CreateCustomer(string email) =>
        Post("/api/customers" + Version, new
        {
            firstName = "Jane",
            lastName = "Doe",
            email,
            shippingAddress = new
            {
                street = "1 Main",
                city = "Seattle",
                state = "WA",
                postalCode = "98101",
                country = "USA",
            },
        }, SalesActor);

    private async Task<Guid> CreateProductWithStock(int quantity)
    {
        var create = await Post("/api/products" + Version, new { productName = "Widget", sku = UniqueSku(), unitPrice = 12.50m }, WarehouseActor);
        create.EnsureSuccessStatusCode();
        var product = await create.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        var productId = product.GetProperty("id").GetGuid();

        var stock = await Post($"/api/products/{productId}/stock-additions{Version}", new { quantity }, WarehouseActor);
        stock.EnsureSuccessStatusCode();

        return productId;
    }

    private async Task<Guid> CreateOrder(Guid customerId, Guid productId, int quantity, string actor)
    {
        var response = await Post("/api/orders" + Version, new
        {
            customerId,
            lineItems = new[] { new { productId, quantity } },
        }, actor);
        response.EnsureSuccessStatusCode();
        var order = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        return order.GetProperty("id").GetGuid();
    }

    private async Task<HttpResponseMessage> Get(string uri, string actor)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Add("X-Test-Actor", actor);
        return await _client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private async Task<HttpResponseMessage> Post(string uri, object? body, string actor)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = body is null ? null : JsonContent.Create(body),
        };
        request.Headers.Add("X-Test-Actor", actor);
        return await _client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static string UniqueEmail() => $"jane-{Guid.NewGuid():N}@example.com";

    private static string UniqueSku() => Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
}
