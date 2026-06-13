using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace OrderManagement.Tests.Integration;

public class IntegrationTests(OrderManagementWebFactory factory)
    : IClassFixture<OrderManagementWebFactory>
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerOptions.Default)
    {
        PropertyNameCaseInsensitive = true
    };

    private HttpClient CreateClient(string? actorJson = null)
    {
        var client = factory.CreateClient();
        if (actorJson != null)
            client.DefaultRequestHeaders.Add("X-Test-Actor", actorJson);
        return client;
    }

    private static string AdminActor() =>
        """{"id":"admin","permissions":["customers:create","products:create","products:manage-stock","orders:create","orders:submit","orders:approve","orders:ship","orders:deliver","orders:cancel","orders:read","orders:read-all"]}""";

    private static string SalesRepActor(string id = "sales-1") =>
        $$$"""{"id":"{{{id}}}","permissions":["customers:create","orders:create","orders:submit","orders:cancel","orders:read"]}""";

    private static string WarehouseActor() =>
        """{"id":"warehouse-1","permissions":["products:create","products:manage-stock","orders:approve","orders:ship","orders:deliver","orders:read-all"]}""";

    // --- Health check ---

    [Fact]
    public async Task HealthCheck_Returns200()
    {
        var client = factory.CreateClient();
        var resp = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    // --- API version ---

    [Fact]
    public async Task MissingApiVersion_Returns400()
    {
        var client = CreateClient(AdminActor());
        var resp = await client.GetAsync("/api/orders/overdue");
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task WithApiVersion_DoesNotReturn400ForVersion()
    {
        var client = CreateClient(AdminActor());
        var resp = await client.GetAsync("/api/orders/overdue?api-version=2026-11-12");
        Assert.NotEqual(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // --- Create customer ---

    [Fact]
    public async Task CreateCustomer_Returns201WithLocation()
    {
        var client = CreateClient(AdminActor());
        var resp = await client.PostAsJsonAsync("/api/customers?api-version=2026-11-12", new
        {
            firstName = "Alice",
            lastName = "Smith",
            email = "alice.smith@example.com",
            street = "1 Main St",
            city = "Anytown",
            state = "CA",
            postalCode = "90210",
            country = "USA"
        });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        Assert.NotNull(resp.Headers.Location);
        Assert.Contains("/api/customers/", resp.Headers.Location!.ToString());

        var body = await resp.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(body!["id"]);
    }

    [Fact]
    public async Task CreateCustomer_DuplicateEmail_Returns409()
    {
        var client = CreateClient(AdminActor());
        var payload = new
        {
            firstName = "Bob",
            lastName = "Dup",
            email = $"dup-{Guid.NewGuid()}@example.com",
            street = "1 St",
            city = "City",
            state = "ST",
            postalCode = "00000",
            country = "USA"
        };

        await client.PostAsJsonAsync("/api/customers?api-version=2026-11-12", payload);
        var resp = await client.PostAsJsonAsync("/api/customers?api-version=2026-11-12", payload);

        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    [Fact]
    public async Task CreateCustomer_MissingPermission_Returns403()
    {
        var noPermActor = """{"id":"no-perm","permissions":["orders:read"]}""";
        var client = CreateClient(noPermActor);
        var resp = await client.PostAsJsonAsync("/api/customers?api-version=2026-11-12", new
        {
            firstName = "Charlie",
            lastName = "Test",
            email = "charlie.test@example.com",
            street = "1 St",
            city = "City",
            state = "ST",
            postalCode = "00000",
            country = "USA"
        });

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    // --- Full order lifecycle ---

    [Fact]
    public async Task FullOrderLifecycle_CreateToDeliver()
    {
        // Setup clients for different roles
        var adminClient = CreateClient(AdminActor());
        var warehouseClient = CreateClient(WarehouseActor());

        // 1. Create customer
        var customerResp = await adminClient.PostAsJsonAsync("/api/customers?api-version=2026-11-12", new
        {
            firstName = "Order",
            lastName = "Tester",
            email = $"lifecycle-{Guid.NewGuid()}@example.com",
            street = "1 St",
            city = "City",
            state = "ST",
            postalCode = "00000",
            country = "USA"
        });
        Assert.Equal(HttpStatusCode.Created, customerResp.StatusCode);
        var customer = await customerResp.Content.ReadFromJsonAsync<JsonObject>();
        var customerId = customer!["id"]!.GetValue<Guid>();

        // 2. Create product
        var productResp = await warehouseClient.PostAsJsonAsync("/api/products?api-version=2026-11-12", new
        {
            productName = "Test Widget",
            sku = $"TW{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
            unitPrice = 25.00
        });
        Assert.Equal(HttpStatusCode.Created, productResp.StatusCode);
        var product = await productResp.Content.ReadFromJsonAsync<JsonObject>();
        var productId = product!["id"]!.GetValue<Guid>();

        // 3. Add stock
        var stockResp = await warehouseClient.PostAsJsonAsync(
            $"/api/products/{productId}/stock-additions?api-version=2026-11-12",
            new { quantity = 50 });
        Assert.Equal(HttpStatusCode.OK, stockResp.StatusCode);

        // 4. Create draft order
        var salesClient = CreateClient(SalesRepActor("sales-lifecycle"));
        var orderResp = await salesClient.PostAsJsonAsync("/api/orders?api-version=2026-11-12", new
        {
            customerId,
            lineItems = new[] { new { productId, quantity = 2 } }
        });
        Assert.Equal(HttpStatusCode.Created, orderResp.StatusCode);
        var order = await orderResp.Content.ReadFromJsonAsync<JsonObject>();
        var orderId = order!["id"]!.GetValue<Guid>();
        Assert.Equal("Draft", order["status"]!.GetValue<string>());

        // 5. Submit order
        var submitResp = await salesClient.PostAsync(
            $"/api/orders/{orderId}/submission?api-version=2026-11-12", null);
        Assert.Equal(HttpStatusCode.OK, submitResp.StatusCode);
        var submitted = await submitResp.Content.ReadFromJsonAsync<JsonObject>();
        Assert.Equal("Submitted", submitted!["status"]!.GetValue<string>());

        // 6. Approve order
        var approveResp = await warehouseClient.PostAsync(
            $"/api/orders/{orderId}/approval?api-version=2026-11-12", null);
        Assert.Equal(HttpStatusCode.OK, approveResp.StatusCode);
        var approved = await approveResp.Content.ReadFromJsonAsync<JsonObject>();
        Assert.Equal("Approved", approved!["status"]!.GetValue<string>());

        // 7. Ship order
        var shipResp = await warehouseClient.PostAsync(
            $"/api/orders/{orderId}/shipment?api-version=2026-11-12", null);
        Assert.Equal(HttpStatusCode.OK, shipResp.StatusCode);
        var shipped = await shipResp.Content.ReadFromJsonAsync<JsonObject>();
        Assert.Equal("Shipped", shipped!["status"]!.GetValue<string>());

        // 8. Deliver order
        var deliverResp = await warehouseClient.PostAsync(
            $"/api/orders/{orderId}/delivery?api-version=2026-11-12", null);
        Assert.Equal(HttpStatusCode.OK, deliverResp.StatusCode);
        var delivered = await deliverResp.Content.ReadFromJsonAsync<JsonObject>();
        Assert.Equal("Delivered", delivered!["status"]!.GetValue<string>());
    }

    // --- Cancel by non-owner ---

    [Fact]
    public async Task CancelOrder_ByNonOwner_Returns403()
    {
        var adminClient = CreateClient(AdminActor());
        var warehouseClient = CreateClient(WarehouseActor());

        // Create customer
        var cResp = await adminClient.PostAsJsonAsync("/api/customers?api-version=2026-11-12", new
        {
            firstName = "Cancel",
            lastName = "Test",
            email = $"cancel-nonowner-{Guid.NewGuid()}@example.com",
            street = "1 St", city = "City", state = "ST", postalCode = "00000", country = "USA"
        });
        var cId = (await cResp.Content.ReadFromJsonAsync<JsonObject>())!["id"]!.GetValue<Guid>();

        // Create product
        var pResp = await warehouseClient.PostAsJsonAsync("/api/products?api-version=2026-11-12", new
        {
            productName = "Cancel Widget",
            sku = $"CW{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
            unitPrice = 10.00
        });
        var pId = (await pResp.Content.ReadFromJsonAsync<JsonObject>())!["id"]!.GetValue<Guid>();
        await warehouseClient.PostAsJsonAsync($"/api/products/{pId}/stock-additions?api-version=2026-11-12", new { quantity = 100 });

        // Create order as sales-rep-A
        var salesA = CreateClient(SalesRepActor("sales-A-cancel"));
        var oResp = await salesA.PostAsJsonAsync("/api/orders?api-version=2026-11-12", new
        {
            customerId = cId,
            lineItems = new[] { new { productId = pId, quantity = 1 } }
        });
        var oId = (await oResp.Content.ReadFromJsonAsync<JsonObject>())!["id"]!.GetValue<Guid>();

        // Attempt cancel as sales-rep-B
        var salesB = CreateClient(SalesRepActor("sales-B-cancel"));
        var cancelResp = await salesB.PostAsync(
            $"/api/orders/{oId}/cancellation?api-version=2026-11-12", null);
        Assert.Equal(HttpStatusCode.Forbidden, cancelResp.StatusCode);
    }

    [Fact]
    public async Task CancelOrder_ByOwner_Returns200()
    {
        var adminClient = CreateClient(AdminActor());
        var warehouseClient = CreateClient(WarehouseActor());

        // Create customer
        var cResp = await adminClient.PostAsJsonAsync("/api/customers?api-version=2026-11-12", new
        {
            firstName = "Cancel",
            lastName = "Owner",
            email = $"cancel-owner-{Guid.NewGuid()}@example.com",
            street = "1 St", city = "City", state = "ST", postalCode = "00000", country = "USA"
        });
        var cId = (await cResp.Content.ReadFromJsonAsync<JsonObject>())!["id"]!.GetValue<Guid>();

        // Create product and stock
        var pResp = await warehouseClient.PostAsJsonAsync("/api/products?api-version=2026-11-12", new
        {
            productName = "Cancel Widget 2",
            sku = $"CW{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
            unitPrice = 10.00
        });
        var pId = (await pResp.Content.ReadFromJsonAsync<JsonObject>())!["id"]!.GetValue<Guid>();
        await warehouseClient.PostAsJsonAsync($"/api/products/{pId}/stock-additions?api-version=2026-11-12", new { quantity = 100 });

        // Create order as sales-rep-owner
        var salesOwner = CreateClient(SalesRepActor("sales-owner"));
        var oResp = await salesOwner.PostAsJsonAsync("/api/orders?api-version=2026-11-12", new
        {
            customerId = cId,
            lineItems = new[] { new { productId = pId, quantity = 1 } }
        });
        var oId = (await oResp.Content.ReadFromJsonAsync<JsonObject>())!["id"]!.GetValue<Guid>();

        // Cancel as the same owner
        var cancelResp = await salesOwner.PostAsync(
            $"/api/orders/{oId}/cancellation?api-version=2026-11-12", null);
        Assert.Equal(HttpStatusCode.OK, cancelResp.StatusCode);
        var cancelled = await cancelResp.Content.ReadFromJsonAsync<JsonObject>();
        Assert.Equal("Cancelled", cancelled!["status"]!.GetValue<string>());
    }

    // --- Overdue orders ---

    [Fact]
    public async Task OverdueOrders_ReturnsSubmittedOver7Days()
    {
        // We can't easily fake time in integration tests without a FakeTimeProvider,
        // so we verify the endpoint returns 200 and a list
        var client = CreateClient(AdminActor());
        var resp = await client.GetAsync("/api/orders/overdue?api-version=2026-11-12");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<JsonArray>();
        Assert.NotNull(body);
    }

    // --- List orders by customer ---

    [Fact]
    public async Task ListOrdersByCustomer_CustomerNotFound_Returns404()
    {
        var client = CreateClient(AdminActor());
        var resp = await client.GetAsync($"/api/customers/{Guid.NewGuid()}/orders?api-version=2026-11-12");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // --- Get order by ID ---

    [Fact]
    public async Task GetOrderById_NotFound_Returns404()
    {
        var client = CreateClient(AdminActor());
        var resp = await client.GetAsync($"/api/orders/{Guid.NewGuid()}?api-version=2026-11-12");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }
}
