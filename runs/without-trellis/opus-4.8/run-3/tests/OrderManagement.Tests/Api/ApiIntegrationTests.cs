using System.Net;
using System.Net.Http.Json;
using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Contracts;
using static OrderManagement.Tests.Api.ApiTestHelpers;

namespace OrderManagement.Tests.Api;

public class ApiIntegrationTests
{
    [Fact]
    public async Task Health_ReturnsOk()
    {
        using var factory = new OrderManagementApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateCustomer_ReturnsCreatedWithLocation()
    {
        using var factory = new OrderManagementApiFactory();
        var client = factory.ClientFor(Admin);

        var response = await client.PostAsJsonAsync(V("/api/customers"), CustomerRequest("loc@example.com"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var customer = await response.ReadAsync<CustomerResponse>();
        Assert.EndsWith(customer.Id.ToString(), response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task CreateCustomer_DuplicateEmail_ReturnsConflictProblemDetails()
    {
        using var factory = new OrderManagementApiFactory();
        var client = factory.ClientFor(Admin);
        await client.PostAsJsonAsync(V("/api/customers"), CustomerRequest("dup@example.com"));

        var response = await client.PostAsJsonAsync(V("/api/customers"), CustomerRequest("dup@example.com"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("application/problem+json", response.Content.Headers.ContentType!.ToString());
    }

    [Fact]
    public async Task CreateCustomer_MissingPermission_ReturnsForbidden()
    {
        using var factory = new OrderManagementApiFactory();
        var client = factory.ClientFor(new Actor("nobody", []));

        var response = await client.PostAsJsonAsync(V("/api/customers"), CustomerRequest("forbidden@example.com"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task MissingApiVersion_ReturnsBadRequest()
    {
        using var factory = new OrderManagementApiFactory();
        var client = factory.ClientFor(Admin);

        var response = await client.PostAsJsonAsync("/api/customers", CustomerRequest("noversion@example.com"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task FullOrderLifecycle_Succeeds()
    {
        using var factory = new OrderManagementApiFactory();
        var sales = factory.ClientFor(Sales());
        var warehouse = factory.ClientFor(Warehouse());

        var customer = await (await sales.PostAsJsonAsync(V("/api/customers"), CustomerRequest("life@example.com")))
            .ReadAsync<CustomerResponse>();

        var product = await (await warehouse.PostAsJsonAsync(V("/api/products"), ProductRequest("WIDGET01", 9.99m)))
            .ReadAsync<ProductResponse>();

        var stockResp = await warehouse.PostAsJsonAsync(V($"/api/products/{product.Id}/stock-additions"), new AddStockRequest(50));
        Assert.Equal(HttpStatusCode.OK, stockResp.StatusCode);

        var order = await (await sales.PostAsJsonAsync(V("/api/orders"),
            new CreateOrderRequest(customer.Id, [new OrderLineRequest(product.Id, 3)]))).ReadAsync<OrderResponse>();
        Assert.Equal("Draft", order.Status);
        Assert.Equal(29.97m, order.OrderTotal);

        var submitResp = await sales.PostAsync(V($"/api/orders/{order.Id}/submission"), null);
        Assert.Equal(HttpStatusCode.OK, submitResp.StatusCode);
        Assert.Equal("Submitted", (await submitResp.ReadAsync<OrderResponse>()).Status);

        Assert.Equal("Approved", (await (await warehouse.PostAsync(V($"/api/orders/{order.Id}/approval"), null)).ReadAsync<OrderResponse>()).Status);
        Assert.Equal("Shipped", (await (await warehouse.PostAsync(V($"/api/orders/{order.Id}/shipment"), null)).ReadAsync<OrderResponse>()).Status);
        Assert.Equal("Delivered", (await (await warehouse.PostAsync(V($"/api/orders/{order.Id}/delivery"), null)).ReadAsync<OrderResponse>()).Status);

        // Stock was reduced by the submitted quantity.
        var fetched = await (await sales.GetAsync(V($"/api/orders/{order.Id}"))).ReadAsync<OrderResponse>();
        Assert.Equal(3, fetched.LineItems[0].Quantity);
    }

    [Fact]
    public async Task CancelByNonOwner_ReturnsForbidden_AndByOwner_Succeeds()
    {
        using var factory = new OrderManagementApiFactory();
        var owner = factory.ClientFor(Sales("owner-1"));
        var warehouse = factory.ClientFor(Warehouse());

        var customer = await (await owner.PostAsJsonAsync(V("/api/customers"), CustomerRequest("cancel@example.com")))
            .ReadAsync<CustomerResponse>();
        var product = await (await warehouse.PostAsJsonAsync(V("/api/products"), ProductRequest("WIDGET02", 5m, 20)))
            .ReadAsync<ProductResponse>();
        var order = await (await owner.PostAsJsonAsync(V("/api/orders"),
            new CreateOrderRequest(customer.Id, [new OrderLineRequest(product.Id, 2)]))).ReadAsync<OrderResponse>();

        var intruder = factory.ClientFor(new Actor("intruder", [Permissions.OrdersCancel]));
        var forbidden = await intruder.PostAsync(V($"/api/orders/{order.Id}/cancellation"), null);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        var cancelled = await owner.PostAsync(V($"/api/orders/{order.Id}/cancellation"), null);
        Assert.Equal(HttpStatusCode.OK, cancelled.StatusCode);
        Assert.Equal("Cancelled", (await cancelled.ReadAsync<OrderResponse>()).Status);
    }

    [Fact]
    public async Task CancelByAdminNonOwner_Succeeds()
    {
        using var factory = new OrderManagementApiFactory();
        var owner = factory.ClientFor(Sales("owner-1"));
        var warehouse = factory.ClientFor(Warehouse());

        var customer = await (await owner.PostAsJsonAsync(V("/api/customers"), CustomerRequest("admincancel@example.com")))
            .ReadAsync<CustomerResponse>();
        var product = await (await warehouse.PostAsJsonAsync(V("/api/products"), ProductRequest("WIDGET03", 5m, 20)))
            .ReadAsync<ProductResponse>();
        var order = await (await owner.PostAsJsonAsync(V("/api/orders"),
            new CreateOrderRequest(customer.Id, [new OrderLineRequest(product.Id, 2)]))).ReadAsync<OrderResponse>();

        var admin = factory.ClientFor(Admin);
        var cancelled = await admin.PostAsync(V($"/api/orders/{order.Id}/cancellation"), null);

        Assert.Equal(HttpStatusCode.OK, cancelled.StatusCode);
    }

    [Fact]
    public async Task OverdueOrders_ReturnsFilteredList()
    {
        using var factory = new OrderManagementApiFactory();
        var sales = factory.ClientFor(Sales());
        var warehouse = factory.ClientFor(Warehouse());

        var customer = await (await sales.PostAsJsonAsync(V("/api/customers"), CustomerRequest("overdue@example.com")))
            .ReadAsync<CustomerResponse>();
        var product = await (await warehouse.PostAsJsonAsync(V("/api/products"), ProductRequest("WIDGET04", 5m, 20)))
            .ReadAsync<ProductResponse>();

        var overdueOrder = await (await sales.PostAsJsonAsync(V("/api/orders"),
            new CreateOrderRequest(customer.Id, [new OrderLineRequest(product.Id, 1)]))).ReadAsync<OrderResponse>();
        await sales.PostAsync(V($"/api/orders/{overdueOrder.Id}/submission"), null);

        // A second, recent submitted order that should NOT appear as overdue.
        var product2 = await (await warehouse.PostAsJsonAsync(V("/api/products"), ProductRequest("WIDGET05", 5m, 20)))
            .ReadAsync<ProductResponse>();

        // Move time forward 8 days so the first order becomes overdue.
        factory.Clock.Advance(TimeSpan.FromDays(8));

        var recentOrder = await (await sales.PostAsJsonAsync(V("/api/orders"),
            new CreateOrderRequest(customer.Id, [new OrderLineRequest(product2.Id, 1)]))).ReadAsync<OrderResponse>();
        await sales.PostAsync(V($"/api/orders/{recentOrder.Id}/submission"), null);

        var overdueResp = await warehouse.GetAsync(V("/api/orders/overdue"));
        Assert.Equal(HttpStatusCode.OK, overdueResp.StatusCode);
        var overdue = await overdueResp.ReadAsync<List<OrderResponse>>();

        Assert.Contains(overdue, o => o.Id == overdueOrder.Id);
        Assert.DoesNotContain(overdue, o => o.Id == recentOrder.Id);
    }

    [Fact]
    public async Task ListOrdersByCustomer_UnknownCustomer_ReturnsNotFound()
    {
        using var factory = new OrderManagementApiFactory();
        var warehouse = factory.ClientFor(Warehouse());

        var response = await warehouse.GetAsync(V($"/api/customers/{Guid.NewGuid()}/orders"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
