namespace Api.Tests;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using OrderManagement.Domain;

[Collection(TestWebApplicationFactoryCollectionFixture.Id)]
public sealed class OrderManagementApiTests(TestWebApplicationFactoryFixture factory)
{
    private const string Version = "?api-version=2026-11-12";
    private readonly HttpClient _client = factory.CreateClient();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Create_customer_returns_created_with_location_and_duplicate_conflict()
    {
        var response = await _client.PostAsJsonAsync($"/api/customers{Version}", Customer("ada@example.com"), Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var duplicate = await _client.PostAsJsonAsync($"/api/customers{Version}", Customer("ada@example.com"), Ct);
        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
        duplicate.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Missing_permission_returns_forbidden()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/products{Version}")
        {
            Content = JsonContent.Create(Product("FORBID1")),
        };
        request.Headers.Add("X-Test-Actor", """{"id":"sales","permissions":["orders:create"]}""");

        var response = await _client.SendAsync(request, Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Full_order_lifecycle_succeeds()
    {
        var customerId = await CreateCustomerAsync("life@example.com");
        var productId = await CreateProductAsync("LIFE1");
        await _client.PostAsJsonAsync($"/api/products/{productId}/stock-additions{Version}", new { quantity = 10 }, Ct);
        var orderId = await CreateOrderAsync(customerId, productId, 2);

        foreach (var path in new[] { "submission", "approval", "shipment", "delivery" })
        {
            var response = await _client.PostAsync($"/api/orders/{orderId}/{path}{Version}", null, Ct);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var get = await _client.GetFromJsonAsync<JsonElement>($"/api/orders/{orderId}{Version}", Ct);
        get.GetProperty("status").GetString().Should().Be("Delivered");
    }

    [Fact]
    public async Task Cancel_by_non_owner_is_forbidden_and_owner_succeeds()
    {
        var customerId = await CreateCustomerAsync("cancel@example.com");
        var productId = await CreateProductAsync("CANCEL1");
        await _client.PostAsJsonAsync($"/api/products/{productId}/stock-additions{Version}", new { quantity = 10 }, Ct);
        var orderId = await CreateOrderAsync(customerId, productId, 1, "owner");

        using var nonOwner = new HttpRequestMessage(HttpMethod.Post, $"/api/orders/{orderId}/cancellation{Version}");
        nonOwner.Headers.Add("X-Test-Actor", """{"id":"other","permissions":["orders:cancel"]}""");
        var forbidden = await _client.SendAsync(nonOwner, Ct);
        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var owner = new HttpRequestMessage(HttpMethod.Post, $"/api/orders/{orderId}/cancellation{Version}");
        owner.Headers.Add("X-Test-Actor", """{"id":"owner","permissions":["orders:cancel"]}""");
        var ok = await _client.SendAsync(owner, Ct);
        ok.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Overdue_orders_and_missing_version_behave_as_specified()
    {
        var overdue = await _client.GetAsync($"/api/orders/overdue{Version}", Ct);
        overdue.StatusCode.Should().Be(HttpStatusCode.OK);

        var missingVersion = await _client.GetAsync("/api/orders/overdue", Ct);
        missingVersion.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Health_returns_ok()
    {
        var response = await _client.GetAsync("/health", Ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<Guid> CreateCustomerAsync(string email)
    {
        var response = await _client.PostAsJsonAsync($"/api/customers{Version}", Customer(email), Ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("id").GetGuid();
    }

    private async Task<Guid> CreateProductAsync(string sku)
    {
        var response = await _client.PostAsJsonAsync($"/api/products{Version}", Product(sku), Ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("id").GetGuid();
    }

    private async Task<Guid> CreateOrderAsync(Guid customerId, Guid productId, int quantity, string actorId = "admin")
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/orders{Version}")
        {
            Content = JsonContent.Create(new
            {
                customerId,
                lineItems = new[] { new { productId, quantity } },
            }),
        };
        request.Headers.Add("X-Test-Actor", $$"""{"id":"{{actorId}}","permissions":["orders:create","orders:read"]}""");
        var response = await _client.SendAsync(request, Ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("id").GetGuid();
    }

    private static object Customer(string email) => new
    {
        firstName = "Ada",
        lastName = "Lovelace",
        email,
        phoneNumber = (string?)null,
        shippingAddress = new
        {
            street = "1 Main St",
            city = "Seattle",
            state = "WA",
            postalCode = "98101",
            country = "USA",
        },
    };

    private static object Product(string sku) => new
    {
        productName = "Widget",
        sku,
        unitPrice = 12.50m,
    };
}
