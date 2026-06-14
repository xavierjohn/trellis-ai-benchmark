namespace Api.Tests;

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

[Collection(TestWebApplicationFactoryCollectionFixture.Id)]
public class OrdersApiTests
{
    private const string V = "api-version=2026-11-12";
    private readonly TestWebApplicationFactoryFixture _factory;

    public OrdersApiTests(TestWebApplicationFactoryFixture factory, ITestOutputHelper output)
    {
        _factory = factory;
        _factory.OutputHelper = output;
    }

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    private HttpClient Client(string actorId = "admin", params string[] permissions) =>
        permissions.Length == 0
            ? _factory.CreateClientWithActor(actorId, Permissions())
            : _factory.CreateClientWithActor(actorId, permissions);

    private static string[] Permissions() => OrderManagement.Domain.Permissions.All.ToArray();

    private static object SampleCustomer(string email = "alice@example.com") => new
    {
        firstName = "Alice",
        lastName = "Smith",
        email,
        phone = "+15551234567",
        shippingAddress = new { street = "1 Main", city = "Redmond", state = "WA", postalCode = "98052", country = "US" },
    };

    private static async Task<Guid> IdOf(HttpResponseMessage response)
    {
        var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<string> StatusOf(HttpResponseMessage response)
    {
        var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("status").GetString()!;
    }

    private static Task<HttpResponseMessage> Post(HttpClient c, string path, object? body) =>
        c.PostAsync($"api/{path}?{V}", body is null ? null : JsonContent.Create(body), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Health_check_returns_200()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("health", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_customer_returns_201_with_location()
    {
        var client = Client();

        var response = await Post(client, "customers", SampleCustomer(Guid.NewGuid() + "@x.com"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task Duplicate_email_returns_409()
    {
        var client = Client();
        var email = Guid.NewGuid() + "@dup.com";
        (await Post(client, "customers", SampleCustomer(email))).StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await Post(client, "customers", SampleCustomer(email));

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        second.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Missing_permission_returns_403()
    {
        var client = Client("nobody", "orders:read");

        var response = await Post(client, "customers", SampleCustomer(Guid.NewGuid() + "@x.com"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Missing_api_version_returns_400()
    {
        var client = Client();

        var response = await client.PostAsync(
            "api/customers", JsonContent.Create(SampleCustomer(Guid.NewGuid() + "@x.com")),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Invalid_email_returns_422()
    {
        var client = Client();
        var bad = new
        {
            firstName = "Alice",
            lastName = "Smith",
            email = "not-an-email",
            shippingAddress = new { street = "1 Main", city = "Redmond", state = "WA", postalCode = "98052", country = "US" },
        };

        var response = await Post(client, "customers", bad);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Full_order_lifecycle_succeeds()
    {
        var client = Client();

        var custResp = await Post(client, "customers", SampleCustomer(Guid.NewGuid() + "@life.com"));
        var customerId = await IdOf(custResp);

        var prodResp = await Post(client, "products", new { name = "Widget", sku = NewSku(), unitPrice = 9.99 });
        var productId = await IdOf(prodResp);

        (await Post(client, $"products/{productId}/stock-additions", new { quantity = 50 }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var orderResp = await Post(client, "orders", new
        {
            customerId,
            lines = new[] { new { productId, quantity = 2 } },
        });
        orderResp.StatusCode.Should().Be(HttpStatusCode.Created);
        orderResp.Headers.Location.Should().NotBeNull();
        var orderId = await IdOf(orderResp);

        (await StatusOf(await Post(client, $"orders/{orderId}/submission", null))).Should().Be("Submitted");
        (await StatusOf(await Post(client, $"orders/{orderId}/approval", null))).Should().Be("Approved");
        (await StatusOf(await Post(client, $"orders/{orderId}/shipment", null))).Should().Be("Shipped");
        (await StatusOf(await Post(client, $"orders/{orderId}/delivery", null))).Should().Be("Delivered");
    }

    [Fact]
    public async Task Cancel_by_non_owner_returns_403_and_owner_succeeds()
    {
        var admin = Client();
        var custResp = await Post(admin, "customers", SampleCustomer(Guid.NewGuid() + "@cancel.com"));
        var customerId = await IdOf(custResp);
        var prodResp = await Post(admin, "products", new { name = "Widget", sku = NewSku(), unitPrice = 5.0 });
        var productId = await IdOf(prodResp);

        var ownerClient = Client("sales-1", "orders:create", "orders:cancel", "orders:read");
        var orderResp = await Post(ownerClient, "orders", new
        {
            customerId,
            lines = new[] { new { productId, quantity = 1 } },
        });
        var orderId = await IdOf(orderResp);

        var nonOwner = Client("sales-2", "orders:cancel");
        (await Post(nonOwner, $"orders/{orderId}/cancellation", null))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        (await Post(ownerClient, $"orders/{orderId}/cancellation", null))
            .StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Overdue_orders_query_returns_200()
    {
        var client = Client("wh", "orders:read-all");

        var response = await client.GetAsync($"api/orders/overdue?{V}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_unknown_order_returns_404()
    {
        var client = Client();

        var response = await client.GetAsync($"api/orders/{Guid.NewGuid()}?{V}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static string NewSku()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var sb = new StringBuilder("SKU");
        var g = Guid.NewGuid().ToByteArray();
        for (var i = 0; i < 8; i++)
            sb.Append(chars[g[i] % chars.Length]);
        return sb.ToString();
    }
}
