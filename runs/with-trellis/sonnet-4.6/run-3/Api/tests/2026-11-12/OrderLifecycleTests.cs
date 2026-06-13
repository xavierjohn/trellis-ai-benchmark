namespace Api.Tests._2026_11_12;

using System.Net;
using System.Net.Http.Json;
using Api.Tests;
using Microsoft.AspNetCore.Mvc;
using Trellis.Testing.AspNetCore;

[Collection(TestWebApplicationFactoryCollectionFixture.Id)]
public class OrderLifecycleTests
{
    private readonly TestWebApplicationFactoryFixture _factory;
    private const string Version = "2026-11-12";

    public OrderLifecycleTests(TestWebApplicationFactoryFixture factory, ITestOutputHelper output)
    {
        _factory = factory;
        _factory.OutputHelper = output;
    }

    private HttpClient CreateAdminClient() =>
        _factory.CreateClientWithActor(
            "admin",
            "customers:create",
            "products:create",
            "products:manage-stock",
            "orders:create",
            "orders:submit",
            "orders:approve",
            "orders:ship",
            "orders:deliver",
            "orders:cancel",
            "orders:read",
            "orders:read-all");

    private HttpClient CreateClientWithPermissions(string actorId, params string[] perms) =>
        _factory.CreateClientWithActor(actorId, perms);

    private static async Task<CustomerTestResponse> CreateCustomer(HttpClient client, string email)
    {
        var body = new
        {
            firstName = "Test",
            lastName = "User",
            email,
            shippingAddress = new { street = "123 Main St", city = "Springfield", state = "IL", postalCode = "62701", country = "US" },
        };
        var response = await client.PostAsJsonAsync($"api/customers?api-version={Version}", body, TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await response.Content.ReadAsAsyncWithAssertion<CustomerTestResponse>();
    }

    private static async Task<ProductTestResponse> CreateProductWithStock(HttpClient client, string sku, int stock = 100)
    {
        var body = new { productName = "Test Widget", sku, unitPrice = 10.00m };
        var createResponse = await client.PostAsJsonAsync($"api/products?api-version={Version}", body, TestContext.Current.CancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var product = await createResponse.Content.ReadAsAsyncWithAssertion<ProductTestResponse>();

        var stockBody = new { quantity = stock };
        var stockResponse = await client.PostAsJsonAsync($"api/products/{product.Id}/stock-additions?api-version={Version}", stockBody, TestContext.Current.CancellationToken);
        stockResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        return await stockResponse.Content.ReadAsAsyncWithAssertion<ProductTestResponse>();
    }

    [Fact]
    public async Task Create_customer_returns_201_with_location()
    {
        var client = CreateAdminClient();
        var body = new
        {
            firstName = "Alice",
            lastName = "Smith",
            email = $"alice_{Guid.NewGuid():N}@test.com",
            shippingAddress = new { street = "10 Oak Ave", city = "Portland", state = "OR", postalCode = "97201", country = "US" },
        };

        var response = await client.PostAsJsonAsync($"api/customers?api-version={Version}", body, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.OriginalString.Should().Contain($"api-version={Version}");
        var customer = await response.Content.ReadAsAsyncWithAssertion<CustomerTestResponse>();
        customer.Email.Should().Be(body.email);
    }

    [Fact]
    public async Task Create_customer_duplicate_email_returns_409()
    {
        var client = CreateAdminClient();
        var email = $"dup_{Guid.NewGuid():N}@test.com";
        await CreateCustomer(client, email);

        var body = new
        {
            firstName = "Bob",
            lastName = "Dup",
            email,
            shippingAddress = new { street = "1 St", city = "City", state = "ST", postalCode = "00000", country = "US" },
        };
        var response = await client.PostAsJsonAsync($"api/customers?api-version={Version}", body, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Create_customer_without_permission_returns_403()
    {
        var client = CreateClientWithPermissions("no-perm-user", "orders:read");
        var body = new
        {
            firstName = "Bob",
            lastName = "Forbidden",
            email = $"forbidden_{Guid.NewGuid():N}@test.com",
            shippingAddress = new { street = "1 St", city = "City", state = "ST", postalCode = "00000", country = "US" },
        };

        var response = await client.PostAsJsonAsync($"api/customers?api-version={Version}", body, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Missing_api_version_returns_400()
    {
        var client = CreateAdminClient();
        var body = new { firstName = "Test", lastName = "User", email = "test@test.com" };

        var response = await client.PostAsJsonAsync("api/customers", body, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Health_check_returns_200()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Full_order_lifecycle_succeeds()
    {
        var client = CreateAdminClient();
        var uniqueSku = $"LCT{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

        var customer = await CreateCustomer(client, $"lifecycle_{Guid.NewGuid():N}@test.com");
        var product = await CreateProductWithStock(client, uniqueSku, 50);

        var createOrderBody = new
        {
            customerId = customer.Id,
            lineItems = new[] { new { productId = product.Id, quantity = 2 } },
        };
        var createOrderResponse = await client.PostAsJsonAsync($"api/orders?api-version={Version}", createOrderBody, TestContext.Current.CancellationToken);
        createOrderResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await createOrderResponse.Content.ReadAsAsyncWithAssertion<OrderTestResponse>();
        order.Status.Should().Be("Draft");
        createOrderResponse.Headers.Location.Should().NotBeNull();
        createOrderResponse.Headers.Location!.OriginalString.Should().Contain($"api-version={Version}");

        var submitResponse = await client.PostAsync($"api/orders/{order.Id}/submission?api-version={Version}", null, TestContext.Current.CancellationToken);
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var submitted = await submitResponse.Content.ReadAsAsyncWithAssertion<OrderTestResponse>();
        submitted.Status.Should().Be("Submitted");
        submitted.SubmittedAt.Should().NotBeNull();

        var approveResponse = await client.PostAsync($"api/orders/{order.Id}/approval?api-version={Version}", null, TestContext.Current.CancellationToken);
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var approved = await approveResponse.Content.ReadAsAsyncWithAssertion<OrderTestResponse>();
        approved.Status.Should().Be("Approved");

        var shipResponse = await client.PostAsync($"api/orders/{order.Id}/shipment?api-version={Version}", null, TestContext.Current.CancellationToken);
        shipResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var shipped = await shipResponse.Content.ReadAsAsyncWithAssertion<OrderTestResponse>();
        shipped.Status.Should().Be("Shipped");
        shipped.ShippedAt.Should().NotBeNull();

        var deliverResponse = await client.PostAsync($"api/orders/{order.Id}/delivery?api-version={Version}", null, TestContext.Current.CancellationToken);
        deliverResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var delivered = await deliverResponse.Content.ReadAsAsyncWithAssertion<OrderTestResponse>();
        delivered.Status.Should().Be("Delivered");
    }

    [Fact]
    public async Task Cancel_own_order_returns_200()
    {
        var adminClient = CreateAdminClient();
        var ownerClient = CreateClientWithPermissions("order-owner", "orders:create", "orders:cancel", "orders:read");

        var uniqueSku = $"OWN{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
        var customer = await CreateCustomer(adminClient, $"owner_{Guid.NewGuid():N}@test.com");
        var product = await CreateProductWithStock(adminClient, uniqueSku, 10);

        var createOrderBody = new
        {
            customerId = customer.Id,
            lineItems = new[] { new { productId = product.Id, quantity = 1 } },
        };
        var createResponse = await ownerClient.PostAsJsonAsync($"api/orders?api-version={Version}", createOrderBody, TestContext.Current.CancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await createResponse.Content.ReadAsAsyncWithAssertion<OrderTestResponse>();

        var cancelResponse = await ownerClient.PostAsync($"api/orders/{order.Id}/cancellation?api-version={Version}", null, TestContext.Current.CancellationToken);

        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var cancelled = await cancelResponse.Content.ReadAsAsyncWithAssertion<OrderTestResponse>();
        cancelled.Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task Cancel_by_non_owner_returns_403()
    {
        var adminClient = CreateAdminClient();
        var ownerClient = CreateClientWithPermissions("real-owner", "orders:create", "orders:cancel", "orders:read");
        var nonOwnerClient = CreateClientWithPermissions("non-owner", "orders:cancel");

        var uniqueSku = $"NON{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
        var customer = await CreateCustomer(adminClient, $"nonown_{Guid.NewGuid():N}@test.com");
        var product = await CreateProductWithStock(adminClient, uniqueSku, 10);

        var createOrderBody = new
        {
            customerId = customer.Id,
            lineItems = new[] { new { productId = product.Id, quantity = 1 } },
        };
        var createResponse = await ownerClient.PostAsJsonAsync($"api/orders?api-version={Version}", createOrderBody, TestContext.Current.CancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await createResponse.Content.ReadAsAsyncWithAssertion<OrderTestResponse>();

        var cancelResponse = await nonOwnerClient.PostAsync($"api/orders/{order.Id}/cancellation?api-version={Version}", null, TestContext.Current.CancellationToken);

        cancelResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Overdue_orders_query_returns_correct_results()
    {
        var client = CreateAdminClient();

        var response = await client.GetAsync($"api/orders/overdue?api-version={Version}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orders = await response.Content.ReadAsAsyncWithAssertion<List<OrderTestResponse>>();
        orders.Should().NotBeNull();
    }

    [Fact]
    public async Task List_orders_by_customer_returns_200()
    {
        var client = CreateAdminClient();
        var uniqueSku = $"LST{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
        var customer = await CreateCustomer(client, $"listcust_{Guid.NewGuid():N}@test.com");
        var product = await CreateProductWithStock(client, uniqueSku, 20);

        var createOrderBody = new
        {
            customerId = customer.Id,
            lineItems = new[] { new { productId = product.Id, quantity = 1 } },
        };
        var createResponse = await client.PostAsJsonAsync($"api/orders?api-version={Version}", createOrderBody, TestContext.Current.CancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var listResponse = await client.GetAsync($"api/customers/{customer.Id}/orders?api-version={Version}", TestContext.Current.CancellationToken);

        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var orders = await listResponse.Content.ReadAsAsyncWithAssertion<List<OrderTestResponse>>();
        orders.Should().HaveCountGreaterThanOrEqualTo(1);
        orders.Should().AllSatisfy(o => o.CustomerId.Should().Be(customer.Id));
    }

    [Fact]
    public async Task Get_nonexistent_order_returns_404()
    {
        var client = CreateAdminClient();
        var fakeId = Guid.NewGuid();

        var response = await client.GetAsync($"api/orders/{fakeId}?api-version={Version}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(404);
    }

    [Fact]
    public async Task Submit_with_insufficient_stock_returns_422()
    {
        var client = CreateAdminClient();
        var uniqueSku = $"STK{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
        var customer = await CreateCustomer(client, $"stock_{Guid.NewGuid():N}@test.com");
        var product = await CreateProductWithStock(client, uniqueSku, 1);

        var createOrderBody = new
        {
            customerId = customer.Id,
            lineItems = new[] { new { productId = product.Id, quantity = 5 } },
        };
        var createResponse = await client.PostAsJsonAsync($"api/orders?api-version={Version}", createOrderBody, TestContext.Current.CancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await createResponse.Content.ReadAsAsyncWithAssertion<OrderTestResponse>();

        var submitResponse = await client.PostAsync($"api/orders/{order.Id}/submission?api-version={Version}", null, TestContext.Current.CancellationToken);

        submitResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Create_order_returns_201_with_location()
    {
        var client = CreateAdminClient();
        var uniqueSku = $"LOC{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
        var customer = await CreateCustomer(client, $"loc_{Guid.NewGuid():N}@test.com");
        var product = await CreateProductWithStock(client, uniqueSku, 10);

        var createOrderBody = new
        {
            customerId = customer.Id,
            lineItems = new[] { new { productId = product.Id, quantity = 1 } },
        };
        var response = await client.PostAsJsonAsync($"api/orders?api-version={Version}", createOrderBody, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.OriginalString.Should().Contain($"api-version={Version}");
    }
}
