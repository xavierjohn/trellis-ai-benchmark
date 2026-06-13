namespace Api.Tests;

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

[Collection(TestWebApplicationFactoryCollectionFixture.Id)]
public class ProblemDetailsTests
{
    private readonly TestWebApplicationFactoryFixture _factory;

    public ProblemDetailsTests(TestWebApplicationFactoryFixture factory, ITestOutputHelper output)
    {
        _factory = factory;
        _factory.OutputHelper = output;
    }

    // The UseStatusCodePages middleware (paired with AddProblemDetails) converts the empty-body
    // 415 short-circuit emitted by the input formatter into a ProblemDetails body. Without it,
    // the response would be a bare "415 Unsupported Media Type" with no Content-Type or body —
    // the gap the production-failure audit surfaced.
    [Fact]
    public async Task Wrong_content_type_returns_415_problem_details()
    {
        var client = _factory.CreateClient();
        var body = new StringContent("not json", Encoding.UTF8, "text/plain");
        var request = new HttpRequestMessage(HttpMethod.Post, "api/Customers?api-version=2026-11-12") { Content = body };

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsWithTrace>(TestContext.Current.CancellationToken);
        Assert.NotNull(problem);
        problem.Status.Should().Be(StatusCodes.Status415UnsupportedMediaType);
        problem.TraceId.Should().NotBeNullOrEmpty();
    }

    // The 405 short-circuit emitted by routing must reach UseStatusCodePages so the body
    // carries a ProblemDetails payload. The CustomizeProblemDetails callback additionally
    // surfaces the Allow header (set by routing per RFC 9110 §15.5.6) as a structured
    // "allow" extension so clients that ignore headers still discover the supported methods.
    [Fact]
    public async Task Wrong_method_returns_405_problem_details_with_allow_extension()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(
            HttpMethod.Patch,
            "api/Orders/00000000-0000-0000-0000-000000000001/line-items/00000000-0000-0000-0000-000000000002?api-version=2026-11-12");

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        response.Content.Headers.Allow.Should().NotBeEmpty(
            "RFC 9110 §15.5.6 requires 405 responses to list supported methods via the Allow representation header");

        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        var doc = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        doc.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status405MethodNotAllowed);
        doc.GetProperty("traceId").GetString().Should().NotBeNullOrEmpty();

        doc.TryGetProperty("allow", out var allow).Should().BeTrue("the 405 body should echo the Allow header as a structured array");
        allow.ValueKind.Should().Be(JsonValueKind.Array);
        var allowValues = allow.EnumerateArray().Select(e => e.GetString()).ToArray();
        allowValues.Should().Contain("DELETE");
    }

    // The 404 emitted by routing when no route template matches must reach UseStatusCodePages.
    // Without it, the response is a bare "404 Not Found" with no Content-Type or body — clients
    // cannot distinguish a route miss from any other dead-air response.
    [Fact]
    public async Task Route_miss_returns_404_problem_details_with_trace_id()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("api/this-route-does-not-exist", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsWithTrace>(TestContext.Current.CancellationToken);
        Assert.NotNull(problem);
        problem.Status.Should().Be(StatusCodes.Status404NotFound);
        problem.TraceId.Should().NotBeNullOrEmpty();
    }

    public class ProblemDetailsWithTrace : ProblemDetails
    {
        public string TraceId { get; set; } = string.Empty;
    }
}
