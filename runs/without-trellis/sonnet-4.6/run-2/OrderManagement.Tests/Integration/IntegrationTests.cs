using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Api.Infrastructure;
using Xunit;

namespace OrderManagement.Tests.Integration;

public class OrderManagementIntegrationTests : IClassFixture<OrderManagementWebFactory>
{
    private readonly HttpClient _client;
    private readonly OrderManagementWebFactory _factory;

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public OrderManagementIntegrationTests(OrderManagementWebFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private const string ApiVersion = "?api-version=2026-11-12";
    private const string AdminActor = "{\"id\":\"admin\",\"permissions\":[\"customers:create\",\"products:create\",\"products:manage-stock\",\"orders:create\",\"orders:submit\",\"orders:approve\",\"orders:ship\",\"orders:deliver\",\"orders:cancel\",\"orders:read\",\"orders:read-all\"]}";
    private const string SalesRepActor = "{\"id\":\"sales-1\",\"permissions\":[\"customers:create\",\"orders:create\",\"orders:submit\",\"orders:cancel\",\"orders:read\"]}";
    private const string SalesRep2Actor = "{\"id\":\"sales-2\",\"permissions\":[\"customers:create\",\"orders:create\",\"orders:submit\",\"orders:cancel\",\"orders:read\"]}";
    private const string WarehouseActor = "{\"id\":\"wh-1\",\"permissions\":[\"products:create\",\"products:manage-stock\",\"orders:approve\",\"orders:ship\",\"orders:deliver\",\"orders:read-all\"]}";
    private const string NoPermActor = "{\"id\":\"nobody\",\"permissions\":[]}";

    private HttpRequestMessage BuildRequest(HttpMethod method, string url, object? body = null, string? actor = AdminActor)
    {
        var request = new HttpRequestMessage(method, url);
        if (actor != null)
            request.Headers.Add("X-Test-Actor", actor);
        if (body != null)
            request.Content = JsonContent.Create(body, options: _jsonOptions);
        return request;
    }

    private async Task<T?> ReadAs<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, _jsonOptions);
    }

    [Fact]
    public async Task HealthCheck_Returns200()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task MissingApiVersion_Returns400()
    {
        var response = await _client.SendAsync(BuildRequest(HttpMethod.Get, "/api/orders/overdue"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateCustomer_Returns201WithLocationHeader()
    {
        var body = new
        {
            firstName = "Alice",
            lastName = "Johnson",
            email = $"alice.johnson.{Guid.NewGuid():N}@test.com",
            shippingAddress = new { street = "1 Main St", city = "Springfield", state = "IL", postalCode = "62701", country = "US" }
        };

        var response = await _client.SendAsync(BuildRequest(HttpMethod.Post, $"/api/customers{ApiVersion}", body));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Contains("/api/customers/", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task CreateCustomer_DuplicateEmail_Returns409()
    {
        var email = $"dup.{Guid.NewGuid():N}@test.com";
        var body = new
        {
            firstName = "Bob",
            lastName = "Smith",
            email,
            shippingAddress = new { street = "2 Main St", city = "Chicago", state = "IL", postalCode = "60601", country = "US" }
        };

        await _client.SendAsync(BuildRequest(HttpMethod.Post, $"/api/customers{ApiVersion}", body));
        var response = await _client.SendAsync(BuildRequest(HttpMethod.Post, $"/api/customers{ApiVersion}", body));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateCustomer_MissingPermission_Returns403()
    {
        var body = new
        {
            firstName = "Eve",
            lastName = "Test",
            email = $"eve.{Guid.NewGuid():N}@test.com",
            shippingAddress = new { street = "3 Main St", city = "Chicago", state = "IL", postalCode = "60601", country = "US" }
        };

        var response = await _client.SendAsync(BuildRequest(HttpMethod.Post, $"/api/customers{ApiVersion}", body, NoPermActor));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateCustomer_InvalidEmail_Returns422()
    {
        var body = new
        {
            firstName = "Frank",
            lastName = "Invalid",
            email = "not-an-email",
            shippingAddress = new { street = "4 Main St", city = "Chicago", state = "IL", postalCode = "60601", country = "US" }
        };

        var response = await _client.SendAsync(BuildRequest(HttpMethod.Post, $"/api/customers{ApiVersion}", body));

        Assert.Equal(HttpStatusCode.UnprocessableContent, response.StatusCode);
    }

    [Fact]
    public async Task FullOrderLifecycle_Succeeds()
    {
        // Create customer
        var customerBody = new
        {
            firstName = "Charlie",
            lastName = "Lifecycle",
            email = $"lifecycle.{Guid.NewGuid():N}@test.com",
            shippingAddress = new { street = "10 Oak Ave", city = "Portland", state = "OR", postalCode = "97201", country = "US" }
        };
        var customerResp = await _client.SendAsync(BuildRequest(HttpMethod.Post, $"/api/customers{ApiVersion}", customerBody));
        Assert.Equal(HttpStatusCode.Created, customerResp.StatusCode);
        var customer = await ReadAs<dynamic>(customerResp);
        var customerId = JsonDocument.Parse(await customerResp.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();

        // Create product
        var productBody = new { productName = "Lifecycle Widget", sku = "LIFWGT001", unitPrice = 25.00m };
        var productResp = await _client.SendAsync(BuildRequest(HttpMethod.Post, $"/api/products{ApiVersion}", productBody));
        Assert.Equal(HttpStatusCode.Created, productResp.StatusCode);
        var productId = JsonDocument.Parse(await productResp.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();

        // Add stock
        var stockResp = await _client.SendAsync(BuildRequest(HttpMethod.Post, $"/api/products/{productId}/stock-additions{ApiVersion}", new { quantity = 100 }));
        Assert.Equal(HttpStatusCode.OK, stockResp.StatusCode);

        // Create order
        var orderBody = new { customerId, lineItems = new[] { new { productId, quantity = 2 } } };
        var orderResp = await _client.SendAsync(BuildRequest(HttpMethod.Post, $"/api/orders{ApiVersion}", orderBody));
        Assert.Equal(HttpStatusCode.Created, orderResp.StatusCode);
        var orderId = JsonDocument.Parse(await orderResp.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();

        // Submit
        var submitResp = await _client.SendAsync(BuildRequest(HttpMethod.Post, $"/api/orders/{orderId}/submission{ApiVersion}"));
        Assert.Equal(HttpStatusCode.OK, submitResp.StatusCode);
        var submitDoc = JsonDocument.Parse(await submitResp.Content.ReadAsStringAsync());
        Assert.Equal("Submitted", submitDoc.RootElement.GetProperty("status").GetString());

        // Approve
        var approveResp = await _client.SendAsync(BuildRequest(HttpMethod.Post, $"/api/orders/{orderId}/approval{ApiVersion}"));
        Assert.Equal(HttpStatusCode.OK, approveResp.StatusCode);

        // Ship
        var shipResp = await _client.SendAsync(BuildRequest(HttpMethod.Post, $"/api/orders/{orderId}/shipment{ApiVersion}"));
        Assert.Equal(HttpStatusCode.OK, shipResp.StatusCode);

        // Deliver
        var deliverResp = await _client.SendAsync(BuildRequest(HttpMethod.Post, $"/api/orders/{orderId}/delivery{ApiVersion}"));
        Assert.Equal(HttpStatusCode.OK, deliverResp.StatusCode);
        var deliverDoc = JsonDocument.Parse(await deliverResp.Content.ReadAsStringAsync());
        Assert.Equal("Delivered", deliverDoc.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task CancelOrder_ByNonOwner_Returns403()
    {
        // Admin creates everything
        var customerBody = new
        {
            firstName = "Dana",
            lastName = "Cancel",
            email = $"cancel.{Guid.NewGuid():N}@test.com",
            shippingAddress = new { street = "5 Elm St", city = "Seattle", state = "WA", postalCode = "98101", country = "US" }
        };
        var customerResp = await _client.SendAsync(BuildRequest(HttpMethod.Post, $"/api/customers{ApiVersion}", customerBody));
        var customerId = JsonDocument.Parse(await customerResp.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();

        var productBody = new { productName = "Cancel Widget", sku = $"CANCEL{Guid.NewGuid():N}".Substring(0, 12).ToUpper(), unitPrice = 10.00m };
        var productResp = await _client.SendAsync(BuildRequest(HttpMethod.Post, $"/api/products{ApiVersion}", productBody));
        var productId = JsonDocument.Parse(await productResp.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();

        await _client.SendAsync(BuildRequest(HttpMethod.Post, $"/api/products/{productId}/stock-additions{ApiVersion}", new { quantity = 50 }));

        // Sales-1 creates the order
        var orderBody = new { customerId, lineItems = new[] { new { productId, quantity = 1 } } };
        var orderResp = await _client.SendAsync(BuildRequest(HttpMethod.Post, $"/api/orders{ApiVersion}", orderBody, SalesRepActor));
        var orderId = JsonDocument.Parse(await orderResp.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();

        // Sales-2 (different actor) tries to cancel
        var cancelResp = await _client.SendAsync(BuildRequest(HttpMethod.Post, $"/api/orders/{orderId}/cancellation{ApiVersion}", null, SalesRep2Actor));

        Assert.Equal(HttpStatusCode.Forbidden, cancelResp.StatusCode);
    }

    [Fact]
    public async Task CancelOrder_ByOwner_Returns200()
    {
        var customerBody = new
        {
            firstName = "Frances",
            lastName = "Owner",
            email = $"owner.{Guid.NewGuid():N}@test.com",
            shippingAddress = new { street = "6 Pine St", city = "Denver", state = "CO", postalCode = "80201", country = "US" }
        };
        var customerResp = await _client.SendAsync(BuildRequest(HttpMethod.Post, $"/api/customers{ApiVersion}", customerBody));
        var customerId = JsonDocument.Parse(await customerResp.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();

        var productBody = new { productName = "Owner Widget", sku = $"OWN{Guid.NewGuid():N}".Substring(0, 10).ToUpper(), unitPrice = 10.00m };
        var productResp = await _client.SendAsync(BuildRequest(HttpMethod.Post, $"/api/products{ApiVersion}", productBody));
        var productId = JsonDocument.Parse(await productResp.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();
        await _client.SendAsync(BuildRequest(HttpMethod.Post, $"/api/products/{productId}/stock-additions{ApiVersion}", new { quantity = 50 }));

        var orderBody = new { customerId, lineItems = new[] { new { productId, quantity = 1 } } };
        var orderResp = await _client.SendAsync(BuildRequest(HttpMethod.Post, $"/api/orders{ApiVersion}", orderBody, SalesRepActor));
        var orderId = JsonDocument.Parse(await orderResp.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();

        // Same actor cancels
        var cancelResp = await _client.SendAsync(BuildRequest(HttpMethod.Post, $"/api/orders/{orderId}/cancellation{ApiVersion}", null, SalesRepActor));

        Assert.Equal(HttpStatusCode.OK, cancelResp.StatusCode);
        var doc = JsonDocument.Parse(await cancelResp.Content.ReadAsStringAsync());
        Assert.Equal("Cancelled", doc.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task ListOrdersByCustomer_Returns200()
    {
        var customerBody = new
        {
            firstName = "Greg",
            lastName = "List",
            email = $"listcust.{Guid.NewGuid():N}@test.com",
            shippingAddress = new { street = "7 Maple Rd", city = "Boston", state = "MA", postalCode = "02101", country = "US" }
        };
        var customerResp = await _client.SendAsync(BuildRequest(HttpMethod.Post, $"/api/customers{ApiVersion}", customerBody));
        var customerId = JsonDocument.Parse(await customerResp.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();

        var productBody = new { productName = "List Widget", sku = $"LST{Guid.NewGuid():N}".Substring(0, 10).ToUpper(), unitPrice = 10.00m };
        var productResp = await _client.SendAsync(BuildRequest(HttpMethod.Post, $"/api/products{ApiVersion}", productBody));
        var productId = JsonDocument.Parse(await productResp.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();
        await _client.SendAsync(BuildRequest(HttpMethod.Post, $"/api/products/{productId}/stock-additions{ApiVersion}", new { quantity = 50 }));

        var orderBody = new { customerId, lineItems = new[] { new { productId, quantity = 1 } } };
        await _client.SendAsync(BuildRequest(HttpMethod.Post, $"/api/orders{ApiVersion}", orderBody));

        var listResp = await _client.SendAsync(BuildRequest(HttpMethod.Get, $"/api/customers/{customerId}/orders{ApiVersion}", actor: WarehouseActor));

        Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);
        var orders = JsonDocument.Parse(await listResp.Content.ReadAsStringAsync());
        Assert.True(orders.RootElement.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task OverdueOrders_Returns200WithCorrectList()
    {
        // Create a fresh factory with a fake time provider that we can control
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace TimeProvider with one set 10 days in the future so submitted orders appear overdue
                var fakeTime = new FakeTimeProvider(DateTime.UtcNow.AddDays(10));
                var existing = services.SingleOrDefault(d => d.ServiceType == typeof(TimeProvider));
                if (existing != null) services.Remove(existing);
                services.AddSingleton<TimeProvider>(fakeTime);
            });
        });
        var client = factory.CreateClient();

        // Create customer and product using default client (real time)
        var customerBody = new
        {
            firstName = "Helen",
            lastName = "Overdue",
            email = $"overdue.{Guid.NewGuid():N}@test.com",
            shippingAddress = new { street = "8 Birch Ln", city = "Austin", state = "TX", postalCode = "73301", country = "US" }
        };
        var customerResp = await _client.SendAsync(BuildRequest(HttpMethod.Post, $"/api/customers{ApiVersion}", customerBody));
        var customerId = JsonDocument.Parse(await customerResp.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();

        var productBody = new { productName = "Overdue Widget", sku = $"OVD{Guid.NewGuid():N}".Substring(0, 10).ToUpper(), unitPrice = 10.00m };
        var productResp = await _client.SendAsync(BuildRequest(HttpMethod.Post, $"/api/products{ApiVersion}", productBody));
        var productId = JsonDocument.Parse(await productResp.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();
        await _client.SendAsync(BuildRequest(HttpMethod.Post, $"/api/products/{productId}/stock-additions{ApiVersion}", new { quantity = 50 }));

        var orderBody = new { customerId, lineItems = new[] { new { productId, quantity = 1 } } };
        var orderResp = await _client.SendAsync(BuildRequest(HttpMethod.Post, $"/api/orders{ApiVersion}", orderBody));
        var orderId = JsonDocument.Parse(await orderResp.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();

        // Submit the order using real time (sets SubmittedAt to real now)
        await _client.SendAsync(BuildRequest(HttpMethod.Post, $"/api/orders/{orderId}/submission{ApiVersion}"));

        // Now query overdue using the "future" client (time is 10 days ahead, so submitted 10 days ago is overdue)
        var overdueResp = await client.SendAsync(BuildRequest(HttpMethod.Get, $"/api/orders/overdue{ApiVersion}", actor: WarehouseActor));

        Assert.Equal(HttpStatusCode.OK, overdueResp.StatusCode);
        var overdueOrders = JsonDocument.Parse(await overdueResp.Content.ReadAsStringAsync());
        Assert.True(overdueOrders.RootElement.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task CreateOrder_Returns201WithLocationHeader()
    {
        var customerBody = new
        {
            firstName = "Ivan",
            lastName = "Order",
            email = $"ivan.{Guid.NewGuid():N}@test.com",
            shippingAddress = new { street = "9 Cedar St", city = "Miami", state = "FL", postalCode = "33101", country = "US" }
        };
        var customerResp = await _client.SendAsync(BuildRequest(HttpMethod.Post, $"/api/customers{ApiVersion}", customerBody));
        var customerId = JsonDocument.Parse(await customerResp.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();

        var productBody = new { productName = "Ivan Widget", sku = $"IVN{Guid.NewGuid():N}".Substring(0, 10).ToUpper(), unitPrice = 15.00m };
        var productResp = await _client.SendAsync(BuildRequest(HttpMethod.Post, $"/api/products{ApiVersion}", productBody));
        var productId = JsonDocument.Parse(await productResp.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();
        await _client.SendAsync(BuildRequest(HttpMethod.Post, $"/api/products/{productId}/stock-additions{ApiVersion}", new { quantity = 50 }));

        var orderBody = new { customerId, lineItems = new[] { new { productId, quantity = 1 } } };
        var response = await _client.SendAsync(BuildRequest(HttpMethod.Post, $"/api/orders{ApiVersion}", orderBody));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Contains("/api/orders/", response.Headers.Location!.ToString());
    }
}
