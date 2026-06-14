namespace OrderManagement.Api.Tests;

using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

public sealed class OrderApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public OrderApiTests(WebApplicationFactory<Program> factory) =>
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Fact]
    public async Task Health_returns_ok()
    {
        var response = await _client.GetAsync("/health", Ct);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Missing_api_version_returns_bad_request()
    {
        var response = await _client.GetAsync("/api/orders/overdue", Ct);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_customer_returns_created_with_location()
    {
        var response = await CreateCustomerAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task Missing_permission_returns_forbidden()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/customers?api-version=2026-11-12");
        request.Headers.Add("X-Test-Actor", """{"id":"actor","permissions":[]}""");
        request.Content = JsonContent.Create(CustomerBody());

        var response = await _client.SendAsync(request, Ct);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Full_order_lifecycle_succeeds()
    {
        var customer = await (await CreateCustomerAsync()).Content.ReadFromJsonAsync<CustomerDto>(Ct);
        var product = await (await _client.PostAsJsonAsync("/api/products?api-version=2026-11-12", new
        {
            name = "Widget",
            sku = $"SKU{Random.Shared.Next(100000, 999999)}",
            unitPrice = 10.0m,
        }, Ct)).Content.ReadFromJsonAsync<ProductDto>(Ct);

        await _client.PostAsJsonAsync($"/api/products/{product!.id}/stock-additions?api-version=2026-11-12", new { quantity = 10 }, Ct);
        var orderResponse = await _client.PostAsJsonAsync("/api/orders?api-version=2026-11-12", new
        {
            customerId = customer!.id,
            lineItems = new[] { new { productId = product.id, quantity = 2 } },
        }, Ct);
        orderResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderDto>(Ct);

        foreach (var operation in new[] { "submission", "approval", "shipment", "delivery" })
        {
            var response = await _client.PostAsync($"/api/orders/{order!.id}/{operation}?api-version=2026-11-12", null, Ct);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            order = await response.Content.ReadFromJsonAsync<OrderDto>(Ct);
        }

        order!.status.Should().Be("Delivered");
    }

    private Task<HttpResponseMessage> CreateCustomerAsync() =>
        _client.PostAsJsonAsync("/api/customers?api-version=2026-11-12", CustomerBody(), Ct);

    private static object CustomerBody() => new
    {
        firstName = "Ada",
        lastName = "Lovelace",
        email = $"ada{Guid.NewGuid():N}@example.com",
        phoneNumber = (string?)null,
        shippingAddress = new { street = "1 Main", city = "Seattle", state = "WA", postalCode = "98101", country = "US" },
    };

    private sealed record CustomerDto(Guid id);
    private sealed record ProductDto(Guid id);
    private sealed record OrderDto(Guid id, string status);
}
