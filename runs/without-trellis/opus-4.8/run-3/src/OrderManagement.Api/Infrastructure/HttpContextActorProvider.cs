using System.Text.Json;
using System.Text.Json.Serialization;
using OrderManagement.Application.Abstractions;

namespace OrderManagement.Api.Infrastructure;

/// <summary>
/// Resolves the current <see cref="Actor"/> from the request.
/// Primary mechanism is the <c>X-Test-Actor</c> header carrying a JSON payload
/// (<c>{"id":"actor-1","permissions":["orders:create"]}</c>). When absent, the
/// actor is derived from <c>sub</c>/<c>oid</c> and <c>role</c> claims; if neither is
/// present a default Admin actor is returned so unauthenticated tooling still works.
/// </summary>
public sealed class HttpContextActorProvider(IHttpContextAccessor accessor) : IActorProvider
{
    public const string HeaderName = "X-Test-Actor";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Actor GetCurrentActor()
    {
        var httpContext = accessor.HttpContext;
        if (httpContext is null)
            return Actor.Admin();

        if (httpContext.Request.Headers.TryGetValue(HeaderName, out var headerValues))
        {
            var raw = headerValues.ToString();
            if (!string.IsNullOrWhiteSpace(raw))
            {
                var payload = JsonSerializer.Deserialize<TestActorPayload>(raw, JsonOptions);
                if (payload is not null && !string.IsNullOrWhiteSpace(payload.Id))
                    return new Actor(payload.Id!, payload.Permissions ?? []);
            }
        }

        var identity = httpContext.User.FindFirst("sub")?.Value
            ?? httpContext.User.FindFirst("oid")?.Value;
        if (!string.IsNullOrWhiteSpace(identity))
        {
            var permissions = httpContext.User.FindAll("role").Select(c => c.Value);
            return new Actor(identity!, permissions);
        }

        return Actor.Admin();
    }

    private sealed class TestActorPayload
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("permissions")]
        public string[]? Permissions { get; set; }
    }
}
