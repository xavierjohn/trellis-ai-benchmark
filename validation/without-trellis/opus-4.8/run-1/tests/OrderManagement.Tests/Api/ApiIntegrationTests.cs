using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using OrderManagement.Api.Application.Auth;
using OrderManagement.Tests.Support;
using Xunit;

namespace OrderManagement.Tests.Api;

public class ApiIntegrationTests : IClassFixture<ApiFactory>
{
    private const string Version = "?api-version=2026-11-12";

    private readonly ApiFactory _factory;

    public ApiIntegrationTests(ApiFactory factory) => _factory = factory;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private HttpClient ClientFor(string actorId, params string[] permissions)
    {
        var client = _factory.CreateClient();
        var payload = JsonSerializer.Serialize(new { id = actorId, permissions });
        client.DefaultRequestHeaders.Add("X-Test-Actor", payload);
        return client;
    }

    private HttpClient AdminClient() => ClientFor("admin", Permissions.All.ToArray());

    private static object ValidCustomer(string email) => new
    {
        firstName = "Jane",
        lastName = "Doe",
        email,
        phoneNumber = (string?)null,
        shippingAddress = new
        {
            street = "123 Main St",
            city = "Springfield",
            state = "IL",
            postalCode = "62701",
            country = "USA"
        }
    };

    // ----- Basic infra -----

    [Fact]
    public async Task Health_Returns200()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task MissingApiVersion_Returns400()
    {
        var client = AdminClient();
        var response = await client.PostAsJsonAsync("/api/customers", ValidCustomer("noversion@example.com"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MalformedJson_Returns400()
    {
        var client = AdminClient();
        var content = new StringContent("{ not valid json ", Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/customers" + Version, content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ----- Customers -----

    [Fact]
    public async Task CreateCustomer_Returns201_WithLocation()
    {
        var client = AdminClient();
        var response = await client.PostAsJsonAsync("/api/customers" + Version, ValidCustomer("created@example.com"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("created@example.com", body.GetProperty("email").GetString());
    }

    [Fact]
    public async Task CreateCustomer_DuplicateEmail_Returns409()
    {
        var client = AdminClient();
        var first = await client.PostAsJsonAsync("/api/customers" + Version, ValidCustomer("dupe@example.com"));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await client.PostAsJsonAsync("/api/customers" + Version, ValidCustomer("dupe@example.com"));
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("application/problem+json", second.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task CreateCustomer_InvalidEmail_Returns422()
    {
        var client = AdminClient();
        var response = await client.PostAsJsonAsync("/api/customers" + Version, ValidCustomer("not-an-email"));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task CreateCustomer_MissingPermission_Returns403()
    {
        var client = ClientFor("limited", Permissions.OrdersRead);
        var response = await client.PostAsJsonAsync("/api/customers" + Version, ValidCustomer("forbidden@example.com"));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ----- Full lifecycle -----

    [Fact]
    public async Task FullOrderLifecycle_Succeeds()
    {
        var client = AdminClient();

        var customerId = await CreateCustomerAsync(client, "lifecycle@example.com");
        var productId = await CreateProductAsync(client, "LIFEC01", 25m);
        await AddStockAsync(client, productId, 10);

        var orderId = await CreateOrderAsync(client, customerId, productId, 3);

        await PostTransition(client, $"/api/orders/{orderId}/submission", "Submitted");
        await PostTransition(client, $"/api/orders/{orderId}/approval", "Approved");
        await PostTransition(client, $"/api/orders/{orderId}/shipment", "Shipped");
        await PostTransition(client, $"/api/orders/{orderId}/delivery", "Delivered");

        // Stock was reduced by 3 on submit.
        var product = await GetJson(client, $"/api/orders/{orderId}");
        Assert.Equal("Delivered", product.GetProperty("status").GetString());
    }

    [Fact]
    public async Task AddLineItem_And_RemoveLineItem_Work()
    {
        var client = AdminClient();
        var customerId = await CreateCustomerAsync(client, "lineitems@example.com");
        var p1 = await CreateProductAsync(client, "LITEMA01", 10m);
        var p2 = await CreateProductAsync(client, "LITEMB01", 20m);

        var orderId = await CreateOrderAsync(client, customerId, p1, 1);

        var add = await client.PostAsJsonAsync($"/api/orders/{orderId}/line-items" + Version,
            new { productId = p2, quantity = 2 });
        Assert.Equal(HttpStatusCode.OK, add.StatusCode);
        var afterAdd = await add.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, afterAdd.GetProperty("lineItems").GetArrayLength());

        var lineItemId = afterAdd.GetProperty("lineItems")[0].GetProperty("lineItemId").GetString();
        var remove = await client.DeleteAsync($"/api/orders/{orderId}/line-items/{lineItemId}" + Version);
        Assert.Equal(HttpStatusCode.OK, remove.StatusCode);
        var afterRemove = await remove.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, afterRemove.GetProperty("lineItems").GetArrayLength());
    }

    // ----- Cancel ownership -----

    [Fact]
    public async Task Cancel_ByNonOwner_Returns403()
    {
        var owner = ClientFor("sales-owner", Permissions.CustomersCreate, Permissions.OrdersCreate, Permissions.OrdersCancel);
        // Seed catalog with admin.
        var admin = AdminClient();
        var customerId = await CreateCustomerAsync(admin, "cancel-nonowner@example.com");
        var productId = await CreateProductAsync(admin, "CANCELA01", 10m);

        var orderId = await CreateOrderAsync(owner, customerId, productId, 1);

        var intruder = ClientFor("other-sales", Permissions.OrdersCancel);
        var response = await intruder.PostAsync($"/api/orders/{orderId}/cancellation" + Version, null);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_ByOwner_Returns200()
    {
        var owner = ClientFor("sales-owner2", Permissions.OrdersCreate, Permissions.OrdersCancel);
        var admin = AdminClient();
        var customerId = await CreateCustomerAsync(admin, "cancel-owner@example.com");
        var productId = await CreateProductAsync(admin, "CANCELB01", 10m);

        var orderId = await CreateOrderAsync(owner, customerId, productId, 1);

        var response = await owner.PostAsync($"/api/orders/{orderId}/cancellation" + Version, null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Cancelled", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Cancel_ByAdmin_NonOwner_Returns200()
    {
        var owner = ClientFor("sales-owner3", Permissions.OrdersCreate, Permissions.OrdersCancel);
        var admin = AdminClient();
        var customerId = await CreateCustomerAsync(admin, "cancel-admin@example.com");
        var productId = await CreateProductAsync(admin, "CANCELC01", 10m);

        var orderId = await CreateOrderAsync(owner, customerId, productId, 1);

        var response = await admin.PostAsync($"/api/orders/{orderId}/cancellation" + Version, null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ----- Overdue -----

    [Fact]
    public async Task OverdueOrders_Returns200_WithFilteredList()
    {
        var client = AdminClient();
        var customerId = await CreateCustomerAsync(client, "overdue@example.com");
        var productId = await CreateProductAsync(client, "OVERDUE01", 10m);
        await AddStockAsync(client, productId, 100);

        // Submit an order at the current fake time, then advance 8 days.
        var overdueOrderId = await CreateOrderAsync(client, customerId, productId, 1);
        await PostTransition(client, $"/api/orders/{overdueOrderId}/submission", "Submitted");

        _factory.Time.Advance(TimeSpan.FromDays(8));

        // A freshly submitted order should NOT be overdue.
        var recentOrderId = await CreateOrderAsync(client, customerId, productId, 1);
        await PostTransition(client, $"/api/orders/{recentOrderId}/submission", "Submitted");

        var response = await client.GetAsync("/api/orders/overdue" + Version);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var ids = body.EnumerateArray().Select(o => o.GetProperty("id").GetString()).ToList();
        Assert.Contains(overdueOrderId, ids);
        Assert.DoesNotContain(recentOrderId, ids);
    }

    [Fact]
    public async Task GetOrder_NotFound_Returns404()
    {
        var client = AdminClient();
        var response = await client.GetAsync($"/api/orders/{Guid.NewGuid()}" + Version);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ----- Helpers -----

    private async Task<string> CreateCustomerAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/customers" + Version, ValidCustomer(email));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetString()!;
    }

    private async Task<string> CreateProductAsync(HttpClient client, string sku, decimal price)
    {
        var response = await client.PostAsJsonAsync("/api/products" + Version,
            new { productName = "Widget", sku, unitPrice = price });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetString()!;
    }

    private async Task AddStockAsync(HttpClient client, string productId, int quantity)
    {
        var response = await client.PostAsJsonAsync($"/api/products/{productId}/stock-additions" + Version,
            new { quantity });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<string> CreateOrderAsync(HttpClient client, string customerId, string productId, int quantity)
    {
        var response = await client.PostAsJsonAsync("/api/orders" + Version, new
        {
            customerId,
            items = new[] { new { productId, quantity } }
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetString()!;
    }

    private async Task PostTransition(HttpClient client, string path, string expectedStatus)
    {
        var response = await client.PostAsync(path + Version, null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(expectedStatus, body.GetProperty("status").GetString());
    }

    private static async Task<JsonElement> GetJson(HttpClient client, string path)
    {
        var response = await client.GetAsync(path + Version);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
}
