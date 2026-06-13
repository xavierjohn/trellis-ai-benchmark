namespace Api.Tests._2026_11_12;

using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Time.Testing;
using OrderManagement.Api.v2026_11_12.Models;
using Trellis.Testing.AspNetCore;

[Collection(TestWebApplicationFactoryCollectionFixture.Id)]
public class OrderManagementApiTests
{
    private const string Version = "api-version=2026-11-12";
    private readonly TestWebApplicationFactoryFixture _factory;

    public OrderManagementApiTests(TestWebApplicationFactoryFixture factory, ITestOutputHelper output)
    {
        _factory = factory;
        _factory.OutputHelper = output;
    }

    [Fact]
    public async Task Health_returns_ok()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Missing_api_version_returns_bad_request()
    {
        var client = _factory.CreateClientWithActor("reader-1", OrderManagement.Domain.Permissions.OrdersReadAll);

        var response = await client.GetAsync("/api/orders/overdue", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Full_order_lifecycle_succeeds_end_to_end()
    {
        var adminClient = _factory.CreateClientWithActor(
            "admin-1",
            OrderManagement.Domain.Permissions.CustomersCreate,
            OrderManagement.Domain.Permissions.ProductsCreate,
            OrderManagement.Domain.Permissions.ProductsManageStock,
            OrderManagement.Domain.Permissions.OrdersApprove,
            OrderManagement.Domain.Permissions.OrdersShip,
            OrderManagement.Domain.Permissions.OrdersDeliver,
            OrderManagement.Domain.Permissions.OrdersReadAll);
        var ownerClient = _factory.CreateClientWithActor(
            "owner-1",
            OrderManagement.Domain.Permissions.OrdersCreate,
            OrderManagement.Domain.Permissions.OrdersSubmit,
            OrderManagement.Domain.Permissions.OrdersRead);

        var customer = await CreateCustomerAsync(adminClient, "lifecycle@example.com");
        var product1 = await CreateProductAsync(adminClient, "Widget A", "LIFE001", 12.5m, 20);
        var product2 = await CreateProductAsync(adminClient, "Widget B", "LIFE002", 8.5m, 20);
        var order = await CreateOrderAsync(ownerClient, customer.Id, product1.Id, 2);

        var addLineItemResponse = await ownerClient.PostAsJsonAsync(
            $"/api/orders/{order.Id}/line-items?{Version}",
            new { productId = product2.Id, quantity = 1 },
            TestContext.Current.CancellationToken);
        addLineItemResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var withSecondItem = await addLineItemResponse.Content.ReadAsAsyncWithAssertion<OrderResponse>();
        withSecondItem.LineItems.Should().HaveCount(2);

        var secondLineItemId = withSecondItem.LineItems.Single(lineItem => lineItem.ProductId == product2.Id).Id;
        var removeLineItemResponse = await ownerClient.DeleteAsync(
            $"/api/orders/{order.Id}/line-items/{secondLineItemId}?{Version}",
            TestContext.Current.CancellationToken);
        removeLineItemResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var submitResponse = await ownerClient.PostAsync($"/api/orders/{order.Id}/submission?{Version}", null, TestContext.Current.CancellationToken);
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await submitResponse.Content.ReadAsAsyncWithAssertion<OrderResponse>()).Status.Should().Be("Submitted");

        var approveResponse = await adminClient.PostAsync($"/api/orders/{order.Id}/approval?{Version}", null, TestContext.Current.CancellationToken);
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await approveResponse.Content.ReadAsAsyncWithAssertion<OrderResponse>()).Status.Should().Be("Approved");

        var shipResponse = await adminClient.PostAsync($"/api/orders/{order.Id}/shipment?{Version}", null, TestContext.Current.CancellationToken);
        shipResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await shipResponse.Content.ReadAsAsyncWithAssertion<OrderResponse>()).Status.Should().Be("Shipped");

        var deliverResponse = await adminClient.PostAsync($"/api/orders/{order.Id}/delivery?{Version}", null, TestContext.Current.CancellationToken);
        deliverResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await deliverResponse.Content.ReadAsAsyncWithAssertion<OrderResponse>()).Status.Should().Be("Delivered");

        var getResponse = await ownerClient.GetAsync($"/api/orders/{order.Id}?{Version}", TestContext.Current.CancellationToken);
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadAsAsyncWithAssertion<OrderResponse>();
        fetched.Status.Should().Be("Delivered");
        fetched.LineItems.Should().HaveCount(1);

        var customerOrdersResponse = await ownerClient.GetAsync($"/api/customers/{customer.Id}/orders?{Version}", TestContext.Current.CancellationToken);
        customerOrdersResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var customerOrders = await customerOrdersResponse.Content.ReadAsAsyncWithAssertion<List<OrderResponse>>();
        customerOrders.Should().ContainSingle(existing => existing.Id == order.Id);
    }

    [Fact]
    public async Task Cancel_by_non_owner_without_read_all_returns_forbidden()
    {
        var adminClient = _factory.CreateClientWithActor(
            "admin-1",
            OrderManagement.Domain.Permissions.CustomersCreate,
            OrderManagement.Domain.Permissions.ProductsCreate,
            OrderManagement.Domain.Permissions.ProductsManageStock);
        var ownerClient = _factory.CreateClientWithActor("owner-2", OrderManagement.Domain.Permissions.OrdersCreate);
        var otherClient = _factory.CreateClientWithActor("other-user", OrderManagement.Domain.Permissions.OrdersCancel);

        var customer = await CreateCustomerAsync(adminClient, "cancel@example.com");
        var product = await CreateProductAsync(adminClient, "Widget C", "CANCEL1", 15m, 5);
        var order = await CreateOrderAsync(ownerClient, customer.Id, product.Id, 1);

        var response = await otherClient.PostAsync($"/api/orders/{order.Id}/cancellation?{Version}", null, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Overdue_endpoint_returns_submitted_orders_older_than_seven_days()
    {
        var factory = _factory.WithFakeTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z", CultureInfo.InvariantCulture), out FakeTimeProvider fakeTimeProvider);
        var adminClient = factory.CreateClientWithActor(
            "admin-2",
            OrderManagement.Domain.Permissions.CustomersCreate,
            OrderManagement.Domain.Permissions.ProductsCreate,
            OrderManagement.Domain.Permissions.ProductsManageStock);
        var ownerClient = factory.CreateClientWithActor(
            "owner-overdue",
            OrderManagement.Domain.Permissions.OrdersCreate,
            OrderManagement.Domain.Permissions.OrdersSubmit,
            OrderManagement.Domain.Permissions.OrdersRead);

        var customer = await CreateCustomerAsync(adminClient, "overdue@example.com");
        var product = await CreateProductAsync(adminClient, "Widget D", "OVERDUE1", 9m, 10);
        var order = await CreateOrderAsync(ownerClient, customer.Id, product.Id, 1);
        var submitResponse = await ownerClient.PostAsync($"/api/orders/{order.Id}/submission?{Version}", null, TestContext.Current.CancellationToken);
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        fakeTimeProvider.Advance(TimeSpan.FromDays(8));

        var overdueResponse = await ownerClient.GetAsync($"/api/orders/overdue?{Version}", TestContext.Current.CancellationToken);

        overdueResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var orders = await overdueResponse.Content.ReadAsAsyncWithAssertion<List<OrderResponse>>();
        orders.Should().ContainSingle(existing => existing.Id == order.Id && existing.Status == "Submitted");
    }

    private static async Task<CustomerResponse> CreateCustomerAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/customers?{Version}",
            new
            {
                firstName = "Jane",
                lastName = "Doe",
                email,
                shippingAddress = new
                {
                    street = "1 Main St",
                    city = "Seattle",
                    state = "WA",
                    postalCode = "98101",
                    country = "US",
                },
            },
            TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await response.Content.ReadAsAsyncWithAssertion<CustomerResponse>();
    }

    private static async Task<ProductResponse> CreateProductAsync(HttpClient client, string name, string sku, decimal unitPrice, int stockToAdd)
    {
        var createResponse = await client.PostAsJsonAsync(
            $"/api/products?{Version}",
            new { name, sku, unitPrice },
            TestContext.Current.CancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var product = await createResponse.Content.ReadAsAsyncWithAssertion<ProductResponse>();

        var stockResponse = await client.PostAsJsonAsync(
            $"/api/products/{product.Id}/stock-additions?{Version}",
            new { quantity = stockToAdd },
            TestContext.Current.CancellationToken);
        stockResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        return await stockResponse.Content.ReadAsAsyncWithAssertion<ProductResponse>();
    }

    private static async Task<OrderResponse> CreateOrderAsync(HttpClient client, Guid customerId, Guid productId, int quantity)
    {
        var createResponse = await client.PostAsJsonAsync(
            $"/api/orders?{Version}",
            new
            {
                customerId,
                lineItems = new[]
                {
                    new { productId, quantity },
                },
            },
            TestContext.Current.CancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        return await createResponse.Content.ReadAsAsyncWithAssertion<OrderResponse>();
    }
}
