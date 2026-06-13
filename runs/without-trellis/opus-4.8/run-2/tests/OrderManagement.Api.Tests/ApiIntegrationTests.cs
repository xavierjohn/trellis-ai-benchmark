using System.Net;
using OrderManagement.Application.Abstractions;
using Xunit;

namespace OrderManagement.Api.Tests;

public class ApiIntegrationTests
{
    private static object CustomerPayload(string email = "jane@example.com") => new
    {
        firstName = "Jane",
        lastName = "Doe",
        email,
        phoneNumber = "+1 555 1234",
        shippingAddress = new
        {
            street = "1 Main St",
            city = "Townsville",
            state = "CA",
            postalCode = "90001",
            country = "US"
        }
    };

    private static object ProductPayload(string sku = "SKU123") => new
    {
        productName = "Widget",
        sku,
        unitPrice = 9.99m
    };

    [Fact]
    public async Task Health_returns_200()
    {
        using var factory = new ApiTestFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateCustomer_returns_201_with_location()
    {
        using var factory = new ApiTestFactory();
        var client = factory.CreateClient();

        var response = await client.PostJsonAsync("/api/customers", CustomerPayload(), TestActors.Admin());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var dto = await response.ReadAsync<CustomerDto>();
        Assert.Contains(dto.Id.ToString(), response.Headers.Location!.ToString());
        Assert.Equal("jane@example.com", dto.Email);
    }

    [Fact]
    public async Task CreateCustomer_duplicate_email_returns_409()
    {
        using var factory = new ApiTestFactory();
        var client = factory.CreateClient();

        var first = await client.PostJsonAsync("/api/customers", CustomerPayload(), TestActors.Admin());
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await client.PostJsonAsync("/api/customers", CustomerPayload(), TestActors.Admin());
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("application/problem+json", second.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task CreateCustomer_missing_permission_returns_403()
    {
        using var factory = new ApiTestFactory();
        var client = factory.CreateClient();

        // Warehouse manager does not have customers:create.
        var response = await client.PostJsonAsync("/api/customers", CustomerPayload(), TestActors.Warehouse("wh-1"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateCustomer_invalid_email_returns_400()
    {
        using var factory = new ApiTestFactory();
        var client = factory.CreateClient();

        var payload = new
        {
            firstName = "Jane",
            lastName = "Doe",
            email = "not-an-email",
            shippingAddress = new { street = "1 Main", city = "Town", state = "CA", postalCode = "90001", country = "US" }
        };

        var response = await client.PostJsonAsync("/api/customers", payload, TestActors.Admin());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Missing_api_version_returns_400()
    {
        using var factory = new ApiTestFactory();
        var client = factory.CreateClient();

        // No api-version query parameter.
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/orders/overdue");
        request.Headers.Add("X-Test-Actor", TestActors.Admin());
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Full_order_lifecycle_succeeds()
    {
        using var factory = new ApiTestFactory();
        var client = factory.CreateClient();
        var admin = TestActors.Admin();
        var sales = TestActors.SalesRep("sales-1");

        var customerId = await CreateCustomerAsync(client, admin);
        var productId = await CreateProductWithStockAsync(client, admin, qty: 100);

        // Create draft order as sales rep.
        var createOrder = await client.PostJsonAsync("/api/orders",
            new { customerId, lineItems = new[] { new { productId, quantity = 2 } } }, sales);
        Assert.Equal(HttpStatusCode.Created, createOrder.StatusCode);
        Assert.NotNull(createOrder.Headers.Location);
        var order = await createOrder.ReadAsync<OrderDto>();
        Assert.Equal("Draft", order.Status);
        Assert.Equal("sales-1", order.CreatedByActorId);
        Assert.Equal(19.98m, order.OrderTotal);

        await AssertStatus(client, await client.PostJsonAsync($"/api/orders/{order.Id}/submission", null, admin), "Submitted");
        await AssertStatus(client, await client.PostJsonAsync($"/api/orders/{order.Id}/approval", null, admin), "Approved");
        await AssertStatus(client, await client.PostJsonAsync($"/api/orders/{order.Id}/shipment", null, admin), "Shipped");
        await AssertStatus(client, await client.PostJsonAsync($"/api/orders/{order.Id}/delivery", null, admin), "Delivered");

        // Stock was reserved on submit: 100 - 2 = 98.
        var get = await client.GetAsync($"/api/orders/{order.Id}", admin);
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
    }

    [Fact]
    public async Task Cancel_by_non_owner_returns_403_and_by_owner_returns_200()
    {
        using var factory = new ApiTestFactory();
        var client = factory.CreateClient();
        var admin = TestActors.Admin();
        var owner = TestActors.SalesRep("sales-owner");
        var other = TestActors.SalesRep("sales-other");

        var customerId = await CreateCustomerAsync(client, admin);
        var productId = await CreateProductWithStockAsync(client, admin, qty: 10);

        var createOrder = await client.PostJsonAsync("/api/orders",
            new { customerId, lineItems = new[] { new { productId, quantity = 1 } } }, owner);
        var order = await createOrder.ReadAsync<OrderDto>();

        var byOther = await client.PostJsonAsync($"/api/orders/{order.Id}/cancellation", null, other);
        Assert.Equal(HttpStatusCode.Forbidden, byOther.StatusCode);

        var byOwner = await client.PostJsonAsync($"/api/orders/{order.Id}/cancellation", null, owner);
        Assert.Equal(HttpStatusCode.OK, byOwner.StatusCode);
        var cancelled = await byOwner.ReadAsync<OrderDto>();
        Assert.Equal("Cancelled", cancelled.Status);
    }

    [Fact]
    public async Task Cancel_by_admin_non_owner_returns_200()
    {
        using var factory = new ApiTestFactory();
        var client = factory.CreateClient();
        var admin = TestActors.Admin("admin-x");
        var owner = TestActors.SalesRep("sales-owner");

        var customerId = await CreateCustomerAsync(client, admin);
        var productId = await CreateProductWithStockAsync(client, admin, qty: 10);

        var createOrder = await client.PostJsonAsync("/api/orders",
            new { customerId, lineItems = new[] { new { productId, quantity = 1 } } }, owner);
        var order = await createOrder.ReadAsync<OrderDto>();

        var byAdmin = await client.PostJsonAsync($"/api/orders/{order.Id}/cancellation", null, admin);
        Assert.Equal(HttpStatusCode.OK, byAdmin.StatusCode);
    }

    [Fact]
    public async Task Overdue_orders_query_returns_filtered_list()
    {
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        using var factory = new ApiTestFactory(start);
        var client = factory.CreateClient();
        var admin = TestActors.Admin();

        var customerId = await CreateCustomerAsync(client, admin);
        var productId = await CreateProductWithStockAsync(client, admin, qty: 100);

        // Overdue order: submitted at start.
        var overdueOrder = await (await client.PostJsonAsync("/api/orders",
            new { customerId, lineItems = new[] { new { productId, quantity = 1 } } }, admin)).ReadAsync<OrderDto>();
        await client.PostJsonAsync($"/api/orders/{overdueOrder.Id}/submission", null, admin);

        // Advance 8 days.
        factory.Clock.Advance(TimeSpan.FromDays(8));

        // Recent order: submitted now (not overdue).
        var recentOrder = await (await client.PostJsonAsync("/api/orders",
            new { customerId, lineItems = new[] { new { productId, quantity = 1 } } }, admin)).ReadAsync<OrderDto>();
        await client.PostJsonAsync($"/api/orders/{recentOrder.Id}/submission", null, admin);

        var response = await client.GetAsync("/api/orders/overdue", admin);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var overdue = await response.ReadAsync<List<OrderDto>>();
        Assert.Single(overdue);
        Assert.Equal(overdueOrder.Id, overdue[0].Id);
    }

    [Fact]
    public async Task ListOrdersByCustomer_unknown_customer_returns_404()
    {
        using var factory = new ApiTestFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/customers/{Guid.NewGuid()}/orders", TestActors.Admin());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<Guid> CreateCustomerAsync(HttpClient client, string actor)
    {
        var response = await client.PostJsonAsync("/api/customers", CustomerPayload(), actor);
        response.EnsureSuccessStatusCode();
        return (await response.ReadAsync<CustomerDto>()).Id;
    }

    private static async Task<Guid> CreateProductWithStockAsync(HttpClient client, string actor, int qty)
    {
        var create = await client.PostJsonAsync("/api/products", ProductPayload(), actor);
        create.EnsureSuccessStatusCode();
        var product = await create.ReadAsync<ProductDto>();

        var addStock = await client.PostJsonAsync($"/api/products/{product.Id}/stock-additions", new { quantity = qty }, actor);
        addStock.EnsureSuccessStatusCode();
        return product.Id;
    }

    private static async Task AssertStatus(HttpClient client, HttpResponseMessage response, string expectedStatus)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var order = await response.ReadAsync<OrderDto>();
        Assert.Equal(expectedStatus, order.Status);
    }
}
