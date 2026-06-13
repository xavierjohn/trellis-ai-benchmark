namespace Api.Tests;

using System.Net;
using OrderManagement.Api.v2026_11_12.Models;
using Trellis.Testing.AspNetCore;

[Collection(TestWebApplicationFactoryCollectionFixture.Id)]
public class OrderLifecycleApiTests(TestWebApplicationFactoryFixture fixture)
{
    private static async Task<(Guid CustomerId, Guid ProductId)> SeedCustomerAndStockedProduct(HttpClient admin, int stock, CancellationToken ct)
    {
        var customerResp = await admin.PostJson("/api/customers", ApiTestHelpers.CustomerBody(ApiTestHelpers.UniqueEmail()), ct);
        customerResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var customer = await customerResp.Content.ReadAsAsyncWithAssertion<CustomerResponse>();

        var productResp = await admin.PostJson("/api/products", ApiTestHelpers.ProductBody(ApiTestHelpers.UniqueSku()), ct);
        productResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var product = await productResp.Content.ReadAsAsyncWithAssertion<ProductResponse>();

        var stockResp = await admin.PostJson($"/api/products/{product.Id}/stock-additions", new { quantity = stock }, ct);
        stockResp.StatusCode.Should().Be(HttpStatusCode.OK);

        return (customer.Id, product.Id);
    }

    [Fact]
    public async Task Full_order_lifecycle_succeeds()
    {
        var ct = TestContext.Current.CancellationToken;
        var admin = fixture.CreateClient();
        var (customerId, productId) = await SeedCustomerAndStockedProduct(admin, 10, ct);

        var createResp = await admin.PostJson("/api/orders", new { customerId, lines = new[] { new { productId, quantity = 2 } } }, ct);
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        createResp.Headers.Location.Should().NotBeNull();
        createResp.Headers.Location!.ToString().Should().Contain("api-version=2026-11-12");
        var order = await createResp.Content.ReadAsAsyncWithAssertion<OrderResponse>();
        order.Status.Should().Be("Draft");
        order.Total.Should().Be(2 * 9.99m);

        var id = order.Id;

        async Task TransitionAndExpect(string path, string expectedStatus)
        {
            var resp = await admin.PostEmpty($"/api/orders/{id}/{path}", ct);
            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            (await resp.Content.ReadAsAsyncWithAssertion<OrderResponse>()).Status.Should().Be(expectedStatus);
        }

        await TransitionAndExpect("submission", "Submitted");
        await TransitionAndExpect("approval", "Approved");
        await TransitionAndExpect("shipment", "Shipped");
        await TransitionAndExpect("delivery", "Delivered");
    }

    [Fact]
    public async Task Submit_with_insufficient_stock_returns_400()
    {
        var ct = TestContext.Current.CancellationToken;
        var admin = fixture.CreateClient();
        var (customerId, productId) = await SeedCustomerAndStockedProduct(admin, 1, ct);

        var createResp = await admin.PostJson("/api/orders", new { customerId, lines = new[] { new { productId, quantity = 5 } } }, ct);
        var order = await createResp.Content.ReadAsAsyncWithAssertion<OrderResponse>();

        var submit = await admin.PostEmpty($"/api/orders/{order.Id}/submission", ct);
        submit.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Invalid_transition_returns_400()
    {
        var ct = TestContext.Current.CancellationToken;
        var admin = fixture.CreateClient();
        var (customerId, productId) = await SeedCustomerAndStockedProduct(admin, 10, ct);

        var createResp = await admin.PostJson("/api/orders", new { customerId, lines = new[] { new { productId, quantity = 1 } } }, ct);
        var order = await createResp.Content.ReadAsAsyncWithAssertion<OrderResponse>();

        // Draft -> Approved is invalid (must submit first).
        var approve = await admin.PostEmpty($"/api/orders/{order.Id}/approval", ct);
        approve.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_order_by_id_returns_200()
    {
        var ct = TestContext.Current.CancellationToken;
        var admin = fixture.CreateClient();
        var (customerId, productId) = await SeedCustomerAndStockedProduct(admin, 10, ct);
        var createResp = await admin.PostJson("/api/orders", new { customerId, lines = new[] { new { productId, quantity = 1 } } }, ct);
        var order = await createResp.Content.ReadAsAsyncWithAssertion<OrderResponse>();

        var get = await admin.GetAsync(ApiTestHelpers.Url($"/api/orders/{order.Id}"), ct);
        get.StatusCode.Should().Be(HttpStatusCode.OK);
        (await get.Content.ReadAsAsyncWithAssertion<OrderResponse>()).Id.Should().Be(order.Id);
    }

    [Fact]
    public async Task Cancel_by_non_owner_returns_403_owner_returns_200()
    {
        var ct = TestContext.Current.CancellationToken;
        var admin = fixture.CreateClient();
        var (customerId, productId) = await SeedCustomerAndStockedProduct(admin, 10, ct);

        var owner = fixture.CreateClientWithActor("owner-1", "orders:create", "orders:cancel");
        var createResp = await owner.PostJson("/api/orders", new { customerId, lines = new[] { new { productId, quantity = 1 } } }, ct);
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await createResp.Content.ReadAsAsyncWithAssertion<OrderResponse>();

        var intruder = fixture.CreateClientWithActor("intruder-1", "orders:cancel");
        var forbidden = await intruder.PostEmpty($"/api/orders/{order.Id}/cancellation", ct);
        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var cancelled = await owner.PostEmpty($"/api/orders/{order.Id}/cancellation", ct);
        cancelled.StatusCode.Should().Be(HttpStatusCode.OK);
        (await cancelled.Content.ReadAsAsyncWithAssertion<OrderResponse>()).Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task List_orders_by_customer_returns_200()
    {
        var ct = TestContext.Current.CancellationToken;
        var admin = fixture.CreateClient();
        var (customerId, productId) = await SeedCustomerAndStockedProduct(admin, 10, ct);
        await admin.PostJson("/api/orders", new { customerId, lines = new[] { new { productId, quantity = 1 } } }, ct);

        var list = await admin.GetAsync(ApiTestHelpers.Url($"/api/customers/{customerId}/orders"), ct);
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        var orders = await list.Content.ReadAsAsyncWithAssertion<List<OrderResponse>>();
        orders.Should().ContainSingle(o => o.CustomerId == customerId);
    }

    [Fact]
    public async Task Overdue_orders_returns_200_and_excludes_recent()
    {
        var ct = TestContext.Current.CancellationToken;
        var admin = fixture.CreateClient();
        var (customerId, productId) = await SeedCustomerAndStockedProduct(admin, 10, ct);
        var createResp = await admin.PostJson("/api/orders", new { customerId, lines = new[] { new { productId, quantity = 1 } } }, ct);
        var order = await createResp.Content.ReadAsAsyncWithAssertion<OrderResponse>();
        await admin.PostEmpty($"/api/orders/{order.Id}/submission", ct);

        var overdue = await admin.GetAsync(ApiTestHelpers.Url("/api/orders/overdue"), ct);
        overdue.StatusCode.Should().Be(HttpStatusCode.OK);
        var orders = await overdue.Content.ReadAsAsyncWithAssertion<List<OrderResponse>>();
        orders.Should().NotContain(o => o.Id == order.Id);
    }
}
