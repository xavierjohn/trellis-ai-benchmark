namespace Api.Tests;

using System.Net;
using OrderManagement.Api.v2026_11_12.Models;
using Trellis.Testing.AspNetCore;

[Collection(TestWebApplicationFactoryCollectionFixture.Id)]
public class HealthAndVersioningTests(TestWebApplicationFactoryFixture fixture)
{
    [Fact]
    public async Task Health_returns_200()
    {
        var client = fixture.CreateClient();
        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Missing_api_version_returns_400()
    {
        var client = fixture.CreateClient();
        var response = await client.GetAsync("/api/orders/overdue", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_customer_returns_201_with_location_header()
    {
        var client = fixture.CreateClient();
        var response = await client.PostJson("/api/customers", ApiTestHelpers.CustomerBody(ApiTestHelpers.UniqueEmail()), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("api-version=2026-11-12");
    }

    [Fact]
    public async Task Duplicate_email_returns_409()
    {
        var client = fixture.CreateClient();
        var email = ApiTestHelpers.UniqueEmail();
        var ct = TestContext.Current.CancellationToken;

        (await client.PostJson("/api/customers", ApiTestHelpers.CustomerBody(email), ct))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        var dup = await client.PostJson("/api/customers", ApiTestHelpers.CustomerBody(email), ct);
        dup.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Blank_name_returns_400()
    {
        var client = fixture.CreateClient();
        var body = ApiTestHelpers.CustomerBody(ApiTestHelpers.UniqueEmail());
        var bad = new
        {
            firstName = "",
            lastName = "Doe",
            email = ApiTestHelpers.UniqueEmail(),
            shippingAddress = new { street = "1 Main St", city = "Springfield", state = "IL", postalCode = "62701", country = "USA" },
        };
        var response = await client.PostJson("/api/customers", bad, TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Missing_permission_returns_403()
    {
        var client = fixture.CreateClientWithActor("reader-1", "orders:read");
        var response = await client.PostJson("/api/customers", ApiTestHelpers.CustomerBody(ApiTestHelpers.UniqueEmail()), TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
