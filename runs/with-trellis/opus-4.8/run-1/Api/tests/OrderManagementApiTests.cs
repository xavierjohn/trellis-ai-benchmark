namespace Api.Tests;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using OrderManagement.Domain;
using Trellis.Testing.AspNetCore;

[Collection(TestWebApplicationFactoryCollectionFixture.Id)]
public class OrderManagementApiTests(TestWebApplicationFactoryFixture fixture)
{
    private const string Version = "?api-version=2026-11-12";

    private static readonly string[] SalesPermissions =
    [
        Permissions.CustomersCreate, Permissions.OrdersCreate, Permissions.OrdersSubmit,
        Permissions.OrdersCancel, Permissions.OrdersRead,
    ];

    private static readonly string[] WarehousePermissions =
    [
        Permissions.ProductsCreate, Permissions.ProductsManageStock, Permissions.OrdersApprove,
        Permissions.OrdersShip, Permissions.OrdersDeliver, Permissions.OrdersReadAll,
    ];

    private HttpClient AdminClient() =>
        fixture.CreateClientWithActor("admin-1", [.. Permissions.All]);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Health_Returns200()
    {
        var client = fixture.CreateClient();

        var response = await client.GetAsync("/health", Ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MissingApiVersion_Returns400()
    {
        var client = AdminClient();

        var response = await client.PostAsJsonAsync("/api/customers", NewCustomer(), Ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCustomer_Returns201WithLocation()
    {
        var client = AdminClient();

        var response = await client.PostAsJsonAsync($"/api/customers{Version}", NewCustomer(), Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task DuplicateEmail_Returns409()
    {
        var client = AdminClient();
        var email = $"{Guid.NewGuid():N}@example.com";

        (await client.PostAsJsonAsync($"/api/customers{Version}", NewCustomer(email), Ct))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        var conflict = await client.PostAsJsonAsync($"/api/customers{Version}", NewCustomer(email), Ct);

        conflict.StatusCode.Should().Be(HttpStatusCode.Conflict);
        conflict.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task CreateProduct_MissingPermission_Returns403()
    {
        var client = fixture.CreateClientWithActor("nobody", Permissions.OrdersRead);

        var response = await client.PostAsJsonAsync($"/api/products{Version}", NewProduct(), Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task FullOrderLifecycle_Succeeds()
    {
        var sales = fixture.CreateClientWithActor("sales-1", SalesPermissions);
        var warehouse = fixture.CreateClientWithActor("wh-1", WarehousePermissions);

        var customerId = await CreateCustomerAsync(sales);
        var productId = await CreateProductAsync(warehouse);
        await AddStockAsync(warehouse, productId, 50);

        var orderId = await CreateOrderAsync(sales, customerId, productId, 3);

        await PostTransition(sales, $"/api/orders/{orderId}/submission", "Submitted");
        await PostTransition(warehouse, $"/api/orders/{orderId}/approval", "Approved");
        await PostTransition(warehouse, $"/api/orders/{orderId}/shipment", "Shipped");
        await PostTransition(warehouse, $"/api/orders/{orderId}/delivery", "Delivered");
    }

    [Fact]
    public async Task CancelByNonOwner_Returns403()
    {
        var owner = fixture.CreateClientWithActor("owner-1", SalesPermissions);
        var warehouse = fixture.CreateClientWithActor("wh-1", WarehousePermissions);
        var customerId = await CreateCustomerAsync(owner);
        var productId = await CreateProductAsync(warehouse);
        await AddStockAsync(warehouse, productId, 10);
        var orderId = await CreateOrderAsync(owner, customerId, productId, 1);

        var stranger = fixture.CreateClientWithActor("stranger", Permissions.OrdersCancel);
        var response = await stranger.PostAsync($"/api/orders/{orderId}/cancellation{Version}", null, Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CancelByOwner_Returns200()
    {
        var owner = fixture.CreateClientWithActor("owner-2", SalesPermissions);
        var warehouse = fixture.CreateClientWithActor("wh-1", WarehousePermissions);
        var customerId = await CreateCustomerAsync(owner);
        var productId = await CreateProductAsync(warehouse);
        await AddStockAsync(warehouse, productId, 10);
        var orderId = await CreateOrderAsync(owner, customerId, productId, 1);

        var response = await owner.PostAsync($"/api/orders/{orderId}/cancellation{Version}", null, Ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadStatus(response)).Should().Be("Cancelled");
    }

    [Fact]
    public async Task OverdueOrders_ReturnsSubmittedOlderThan7Days()
    {
        var clockFactory = fixture.WithFakeTimeProvider(
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), out var clock);
        var sales = clockFactory.CreateClientWithActor("sales-od", SalesPermissions);
        var warehouse = clockFactory.CreateClientWithActor("wh-od", WarehousePermissions);

        var customerId = await CreateCustomerAsync(sales);
        var productId = await CreateProductAsync(warehouse);
        await AddStockAsync(warehouse, productId, 10);
        var orderId = await CreateOrderAsync(sales, customerId, productId, 1);
        await PostTransition(sales, $"/api/orders/{orderId}/submission", "Submitted");

        clock.Advance(TimeSpan.FromDays(8));

        var response = await warehouse.GetAsync($"/api/orders/overdue{Version}", Ct);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        doc.RootElement.EnumerateArray().Select(o => o.GetProperty("id").GetGuid())
            .Should().Contain(orderId);
    }

    private static object NewCustomer(string? email = null) => new
    {
        firstName = "Jane",
        lastName = "Doe",
        email = email ?? $"{Guid.NewGuid():N}@example.com",
        shippingAddress = new
        {
            street = "1 Main St",
            city = "Town",
            state = "CA",
            postalCode = "90001",
            country = "US",
        },
    };

    private static object NewProduct(string? sku = null) => new
    {
        name = "Widget",
        sku = sku ?? $"SKU{Random.Shared.Next(100000, 999999)}",
        unitPrice = 9.99m,
    };

    private static async Task<Guid> CreateCustomerAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync($"/api/customers{Version}", NewCustomer(), Ct);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await ReadId(response);
    }

    private static async Task<Guid> CreateProductAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync($"/api/products{Version}", NewProduct(), Ct);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await ReadId(response);
    }

    private static async Task AddStockAsync(HttpClient client, Guid productId, int quantity)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/products/{productId}/stock-additions{Version}", new { quantity }, Ct);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task<Guid> CreateOrderAsync(HttpClient client, Guid customerId, Guid productId, int quantity)
    {
        var body = new { customerId, lines = new[] { new { productId, quantity } } };
        var response = await client.PostAsJsonAsync($"/api/orders{Version}", body, Ct);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await ReadId(response);
    }

    private static async Task PostTransition(HttpClient client, string path, string expectedStatus)
    {
        var response = await client.PostAsync($"{path}{Version}", null, Ct);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadStatus(response)).Should().Be(expectedStatus);
    }

    private static async Task<Guid> ReadId(HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<string> ReadStatus(HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("status").GetString()!;
    }
}
