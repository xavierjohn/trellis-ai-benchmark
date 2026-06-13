using System.Net;
using System.Net.Http.Json;
using OrderManagement.Application.Contracts;
using static OrderManagement.Tests.Api.ApiClient;

namespace OrderManagement.Tests.Api;

public class ApiIntegrationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    // Role-based permission sets (per spec §5.2)
    private static readonly string[] SalesRep =
        { "customers:create", "orders:create", "orders:submit", "orders:cancel", "orders:read" };
    private static readonly string[] Warehouse =
        { "products:create", "products:manage-stock", "orders:approve", "orders:ship", "orders:deliver", "orders:read-all" };
    private static readonly string[] AdminPerms =
    {
        "customers:create", "products:create", "products:manage-stock", "orders:create", "orders:submit",
        "orders:approve", "orders:ship", "orders:deliver", "orders:cancel", "orders:read", "orders:read-all"
    };

    public ApiIntegrationTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static object CustomerBody(string email) => new
    {
        firstName = "Jane",
        lastName = "Doe",
        email,
        phoneNumber = "+1 555 123 4567",
        shippingAddress = new { street = "1 Main", city = "Springfield", state = "IL", postalCode = "62704", country = "USA" }
    };

    private async Task<CustomerResponse> CreateCustomerAsync(string email, string actorId = "admin-1")
    {
        var resp = await _client.SendAsync(Request(HttpMethod.Post, "/api/customers", Actor(actorId, AdminPerms), CustomerBody(email)));
        resp.EnsureSuccessStatusCode();
        return await resp.ReadAsync<CustomerResponse>();
    }

    private async Task<ProductResponse> CreateProductWithStockAsync(string sku, decimal price, int stock)
    {
        var create = await _client.SendAsync(Request(HttpMethod.Post, "/api/products", Actor("wh-1", AdminPerms),
            new { productName = "Widget", sku, unitPrice = price }));
        create.EnsureSuccessStatusCode();
        var product = await create.ReadAsync<ProductResponse>();

        var stockResp = await _client.SendAsync(Request(HttpMethod.Post, $"/api/products/{product.Id}/stock-additions",
            Actor("wh-1", AdminPerms), new { quantity = stock }));
        stockResp.EnsureSuccessStatusCode();
        return await stockResp.ReadAsync<ProductResponse>();
    }

    [Fact]
    public async Task Health_Returns200()
    {
        var resp = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task MissingApiVersion_Returns400()
    {
        var resp = await _client.GetAsync("/api/orders/overdue");
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CreateCustomer_Returns201WithLocation()
    {
        var resp = await _client.SendAsync(Request(HttpMethod.Post, "/api/customers",
            Actor("sales-1", SalesRep), CustomerBody("loc-test@example.com")));

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        Assert.NotNull(resp.Headers.Location);
        var customer = await resp.ReadAsync<CustomerResponse>();
        Assert.Contains(customer.Id.ToString(), resp.Headers.Location!.ToString());
    }

    [Fact]
    public async Task DuplicateEmail_Returns409ProblemDetails()
    {
        await CreateCustomerAsync("dupe@example.com");

        var resp = await _client.SendAsync(Request(HttpMethod.Post, "/api/customers",
            Actor("admin-1", AdminPerms), CustomerBody("dupe@example.com")));

        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
        var problem = await resp.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
        Assert.NotNull(problem);
        Assert.Equal(409, problem!.Status);
    }

    [Fact]
    public async Task MissingPermission_Returns403()
    {
        // Sales rep lacks products:create
        var resp = await _client.SendAsync(Request(HttpMethod.Post, "/api/products",
            Actor("sales-1", SalesRep), new { productName = "Widget", sku = "NOPERM01", unitPrice = 1m }));

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task FullOrderLifecycle_Succeeds()
    {
        var customer = await CreateCustomerAsync("lifecycle@example.com");
        var product = await CreateProductWithStockAsync("LIFE0001", 25m, 100);

        var createResp = await _client.SendAsync(Request(HttpMethod.Post, "/api/orders", Actor("sales-1", SalesRep),
            new { customerId = customer.Id, lines = new[] { new { productId = product.Id, quantity = 4 } } }));
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
        var order = await createResp.ReadAsync<OrderResponse>();
        Assert.Equal("Draft", order.Status);
        Assert.Equal(100m, order.OrderTotal);

        await AssertTransition($"/api/orders/{order.Id}/submission", Warehouse, "Submitted", "sales-1", SalesRep);
        await AssertTransition($"/api/orders/{order.Id}/approval", Warehouse, "Approved");
        await AssertTransition($"/api/orders/{order.Id}/shipment", Warehouse, "Shipped");
        await AssertTransition($"/api/orders/{order.Id}/delivery", Warehouse, "Delivered");

        // Stock was reserved on submit: 100 - 4 = 96
        var getProduct = await _client.SendAsync(Request(HttpMethod.Get, $"/api/orders/{order.Id}", Actor("sales-1", SalesRep)));
        getProduct.EnsureSuccessStatusCode();
    }

    private async Task AssertTransition(string path, string[] perms, string expectedStatus,
        string? submitActorId = null, string[]? submitPerms = null)
    {
        var actorId = path.EndsWith("submission") ? submitActorId ?? "sales-1" : "wh-1";
        var actorPerms = path.EndsWith("submission") ? submitPerms ?? perms : perms;
        var resp = await _client.SendAsync(Request(HttpMethod.Post, path, Actor(actorId, actorPerms)));
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var order = await resp.ReadAsync<OrderResponse>();
        Assert.Equal(expectedStatus, order.Status);
    }

    [Fact]
    public async Task CancelByNonOwner_Returns403_AndByOwner_Returns200()
    {
        var customer = await CreateCustomerAsync("cancel@example.com");
        var product = await CreateProductWithStockAsync("CANCEL01", 10m, 50);

        var createResp = await _client.SendAsync(Request(HttpMethod.Post, "/api/orders", Actor("owner-1", SalesRep),
            new { customerId = customer.Id, lines = new[] { new { productId = product.Id, quantity = 2 } } }));
        var order = await createResp.ReadAsync<OrderResponse>();

        // Non-owner with cancel permission but not admin
        var nonOwner = await _client.SendAsync(Request(HttpMethod.Post, $"/api/orders/{order.Id}/cancellation",
            Actor("intruder", "orders:cancel")));
        Assert.Equal(HttpStatusCode.Forbidden, nonOwner.StatusCode);

        // Owner cancels
        var owner = await _client.SendAsync(Request(HttpMethod.Post, $"/api/orders/{order.Id}/cancellation",
            Actor("owner-1", SalesRep)));
        Assert.Equal(HttpStatusCode.OK, owner.StatusCode);
        var cancelled = await owner.ReadAsync<OrderResponse>();
        Assert.Equal("Cancelled", cancelled.Status);
    }

    [Fact]
    public async Task OverdueOrders_Returns200WithFilteredList()
    {
        var customer = await CreateCustomerAsync("overdue@example.com");
        var product = await CreateProductWithStockAsync("OVERDUE1", 10m, 50);

        var createResp = await _client.SendAsync(Request(HttpMethod.Post, "/api/orders", Actor("sales-1", SalesRep),
            new { customerId = customer.Id, lines = new[] { new { productId = product.Id, quantity = 1 } } }));
        var order = await createResp.ReadAsync<OrderResponse>();

        var submit = await _client.SendAsync(Request(HttpMethod.Post, $"/api/orders/{order.Id}/submission",
            Actor("sales-1", SalesRep)));
        submit.EnsureSuccessStatusCode();

        // Advance time beyond the 7-day threshold.
        _factory.Time.Advance(TimeSpan.FromDays(8));

        var resp = await _client.SendAsync(Request(HttpMethod.Get, "/api/orders/overdue", Actor("wh-1", Warehouse)));
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var overdue = await resp.ReadAsync<List<OrderResponse>>();
        Assert.Contains(overdue, o => o.Id == order.Id);
    }

    [Fact]
    public async Task OverdueOrders_WithoutReadAllPermission_Returns403()
    {
        var resp = await _client.SendAsync(Request(HttpMethod.Get, "/api/orders/overdue", Actor("sales-1", SalesRep)));
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    private sealed record ProblemDetailsResponse(string? Title, int Status, string? Detail);
}
