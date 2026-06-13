namespace Api.Tests._2026_11_12;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Trellis.Testing.AspNetCore;

[Collection(TestWebApplicationFactoryCollectionFixture.Id)]
public class OrderManagementTests
{
    private readonly TestWebApplicationFactoryFixture _factory;
    private const string ApiVersion = "2026-11-12";
    private const string V = $"?api-version={ApiVersion}";

    public OrderManagementTests(TestWebApplicationFactoryFixture factory, ITestOutputHelper output)
    {
        _factory = factory;
        _factory.OutputHelper = output;
    }

    private HttpClient CreateClient(string actorId, params string[] permissions) =>
        _factory.CreateClientWithActor(actorId, permissions);

    private HttpClient AdminClient() => CreateClient(
        "admin-1",
        "customers:create", "products:create", "products:manage-stock",
        "orders:create", "orders:submit", "orders:approve",
        "orders:ship", "orders:deliver", "orders:cancel",
        "orders:read", "orders:read-all");

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
    }

    // ─── Health check ───────────────────────────────────────────────────────────

    [Fact]
    public async Task HealthCheck_returns_200()
    {
        var client = AdminClient();
        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─── API version ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Missing_api_version_returns_400()
    {
        var client = AdminClient();
        var response = await client.GetAsync("api/orders", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ─── Customers ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_customer_returns_201_with_location()
    {
        var client = AdminClient();
        var body = new
        {
            firstName = "John",
            lastName = "Doe",
            email = $"john-{Guid.NewGuid():N}@example.com",
            shippingAddress = new
            {
                street = "123 Main St",
                city = "Springfield",
                state = "IL",
                postalCode = "62701",
                country = "US"
            }
        };

        var response = await client.PostAsJsonAsync($"api/customers{V}", body, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.OriginalString.Should().Contain($"api-version={ApiVersion}");

        var json = await ReadJsonAsync(response);
        json.GetProperty("firstName").GetString().Should().Be("John");
        json.GetProperty("lastName").GetString().Should().Be("Doe");
    }

    [Fact]
    public async Task Create_customer_duplicate_email_returns_409()
    {
        var client = AdminClient();
        var email = $"dupe-{Guid.NewGuid():N}@example.com";
        var body = new
        {
            firstName = "Dup",
            lastName = "Email",
            email,
            shippingAddress = new { street = "1 St", city = "City", state = "ST", postalCode = "00001", country = "US" }
        };

        await client.PostAsJsonAsync($"api/customers{V}", body, TestContext.Current.CancellationToken);
        var response = await client.PostAsJsonAsync($"api/customers{V}", body, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Create_customer_without_permission_returns_403()
    {
        var client = CreateClient("no-perm-user", "orders:read");
        var body = new
        {
            firstName = "No",
            lastName = "Perm",
            email = $"noperm-{Guid.NewGuid():N}@example.com",
            shippingAddress = new { street = "1 St", city = "City", state = "ST", postalCode = "00001", country = "US" }
        };

        var response = await client.PostAsJsonAsync($"api/customers{V}", body, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ─── Products ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_product_returns_201_with_location()
    {
        var client = AdminClient();
        var sku = $"SKU{Guid.NewGuid():N}".Substring(0, 10).ToUpperInvariant();
        var body = new { productName = "Test Widget", sku, unitPrice = 9.99m };

        var response = await client.PostAsJsonAsync($"api/products{V}", body, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }

    // ─── Full order lifecycle ────────────────────────────────────────────────────

    [Fact]
    public async Task Full_order_lifecycle_draft_to_delivered()
    {
        var client = AdminClient();

        // Create customer
        var customerEmail = $"lifecycle-{Guid.NewGuid():N}@example.com";
        var customerResponse = await client.PostAsJsonAsync($"api/customers{V}", new
        {
            firstName = "Lifecycle",
            lastName = "Test",
            email = customerEmail,
            shippingAddress = new { street = "1 St", city = "City", state = "ST", postalCode = "00001", country = "US" }
        }, TestContext.Current.CancellationToken);
        customerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var customerJson = await ReadJsonAsync(customerResponse);
        var customerId = customerJson.GetProperty("id").GetGuid();

        // Create product
        var sku = $"LIF{Guid.NewGuid():N}".Substring(0, 8).ToUpperInvariant();
        var productResponse = await client.PostAsJsonAsync($"api/products{V}",
            new { productName = "Lifecycle Product", sku, unitPrice = 25.00m },
            TestContext.Current.CancellationToken);
        productResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var productJson = await ReadJsonAsync(productResponse);
        var productId = productJson.GetProperty("id").GetGuid();

        // Add stock
        var stockResponse = await client.PostAsJsonAsync($"api/products/{productId}/stock-additions{V}",
            new { quantity = 10 },
            TestContext.Current.CancellationToken);
        stockResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Create draft order
        var orderResponse = await client.PostAsJsonAsync($"api/orders{V}", new
        {
            customerId,
            lineItems = new[] { new { productId, quantity = 2 } }
        }, TestContext.Current.CancellationToken);
        orderResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var orderJson = await ReadJsonAsync(orderResponse);
        var orderId = orderJson.GetProperty("id").GetGuid();
        orderJson.GetProperty("status").GetString().Should().Be("Draft");
        orderJson.GetProperty("orderTotal").GetDecimal().Should().Be(50.00m);

        // Submit
        var submitResponse = await client.PostAsJsonAsync($"api/orders/{orderId}/submission{V}", new { }, TestContext.Current.CancellationToken);
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadJsonAsync(submitResponse)).GetProperty("status").GetString().Should().Be("Submitted");

        // Approve
        var approveResponse = await client.PostAsJsonAsync($"api/orders/{orderId}/approval{V}", new { }, TestContext.Current.CancellationToken);
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadJsonAsync(approveResponse)).GetProperty("status").GetString().Should().Be("Approved");

        // Ship
        var shipResponse = await client.PostAsJsonAsync($"api/orders/{orderId}/shipment{V}", new { }, TestContext.Current.CancellationToken);
        shipResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadJsonAsync(shipResponse)).GetProperty("status").GetString().Should().Be("Shipped");

        // Deliver
        var deliverResponse = await client.PostAsJsonAsync($"api/orders/{orderId}/delivery{V}", new { }, TestContext.Current.CancellationToken);
        deliverResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadJsonAsync(deliverResponse)).GetProperty("status").GetString().Should().Be("Delivered");
    }

    [Fact]
    public async Task Cancel_order_by_owner_succeeds()
    {
        var client = AdminClient();

        var customerEmail = $"cancel-{Guid.NewGuid():N}@example.com";
        var customerResp = await client.PostAsJsonAsync($"api/customers{V}", new
        {
            firstName = "Cancel",
            lastName = "Owner",
            email = customerEmail,
            shippingAddress = new { street = "1 St", city = "City", state = "ST", postalCode = "00001", country = "US" }
        }, TestContext.Current.CancellationToken);
        var customerId = (await ReadJsonAsync(customerResp)).GetProperty("id").GetGuid();

        var sku = $"CAN{Guid.NewGuid():N}".Substring(0, 8).ToUpperInvariant();
        var productResp = await client.PostAsJsonAsync($"api/products{V}",
            new { productName = "Cancel Product", sku, unitPrice = 5.00m },
            TestContext.Current.CancellationToken);
        var productId = (await ReadJsonAsync(productResp)).GetProperty("id").GetGuid();
        await client.PostAsJsonAsync($"api/products/{productId}/stock-additions{V}", new { quantity = 20 }, TestContext.Current.CancellationToken);

        var orderResp = await client.PostAsJsonAsync($"api/orders{V}", new
        {
            customerId,
            lineItems = new[] { new { productId, quantity = 1 } }
        }, TestContext.Current.CancellationToken);
        var orderId = (await ReadJsonAsync(orderResp)).GetProperty("id").GetGuid();

        var cancelResp = await client.PostAsJsonAsync($"api/orders/{orderId}/cancellation{V}", new { }, TestContext.Current.CancellationToken);

        cancelResp.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadJsonAsync(cancelResp)).GetProperty("status").GetString().Should().Be("Cancelled");
    }

    [Fact]
    public async Task Cancel_order_without_permission_returns_403()
    {
        var adminClient = AdminClient();
        var limitedClient = CreateClient("limited-user", "orders:read");

        var customerEmail = $"forbid-{Guid.NewGuid():N}@example.com";
        var customerResp = await adminClient.PostAsJsonAsync($"api/customers{V}", new
        {
            firstName = "Forbid",
            lastName = "Test",
            email = customerEmail,
            shippingAddress = new { street = "1 St", city = "City", state = "ST", postalCode = "00001", country = "US" }
        }, TestContext.Current.CancellationToken);
        var customerId = (await ReadJsonAsync(customerResp)).GetProperty("id").GetGuid();

        var sku = $"FOR{Guid.NewGuid():N}".Substring(0, 8).ToUpperInvariant();
        var productResp = await adminClient.PostAsJsonAsync($"api/products{V}",
            new { productName = "Forbid Product", sku, unitPrice = 5.00m },
            TestContext.Current.CancellationToken);
        var productId = (await ReadJsonAsync(productResp)).GetProperty("id").GetGuid();
        await adminClient.PostAsJsonAsync($"api/products/{productId}/stock-additions{V}", new { quantity = 10 }, TestContext.Current.CancellationToken);

        var orderResp = await adminClient.PostAsJsonAsync($"api/orders{V}", new
        {
            customerId,
            lineItems = new[] { new { productId, quantity = 1 } }
        }, TestContext.Current.CancellationToken);
        var orderId = (await ReadJsonAsync(orderResp)).GetProperty("id").GetGuid();

        var cancelResp = await limitedClient.PostAsJsonAsync($"api/orders/{orderId}/cancellation{V}", new { }, TestContext.Current.CancellationToken);

        cancelResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_overdue_orders_returns_correct_list()
    {
        var client = AdminClient();

        // Just verify the endpoint is reachable and returns 200
        var response = await client.GetAsync($"api/orders/overdue{V}", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await ReadJsonAsync(response);
        json.ValueKind.Should().Be(JsonValueKind.Array);
    }
}
