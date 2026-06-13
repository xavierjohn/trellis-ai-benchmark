using System.Text.Json;
using System.Text.Json.Serialization;
using OrderManagement.Application.Authorization;

namespace OrderManagement.Api.Auth;

/// <summary>
/// Resolves the current <see cref="IActor"/> from the request context.
/// Priority: the <c>X-Test-Actor</c> header (JSON), then standard claims, then a default Admin actor.
/// </summary>
public sealed class HttpActorProvider : IActorProvider
{
    public const string TestActorHeader = "X-Test-Actor";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IHttpContextAccessor _httpContextAccessor;
    private IActor? _cached;

    public HttpActorProvider(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    public IActor Current => _cached ??= Resolve();

    private IActor Resolve()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null)
            return Actor.DefaultAdmin();

        if (context.Request.Headers.TryGetValue(TestActorHeader, out var raw) && !string.IsNullOrWhiteSpace(raw))
        {
            try
            {
                var payload = JsonSerializer.Deserialize<TestActorPayload>(raw.ToString(), JsonOptions);
                if (payload is not null && !string.IsNullOrWhiteSpace(payload.Id))
                    return new Actor(payload.Id, payload.Permissions ?? Array.Empty<string>());
            }
            catch (JsonException)
            {
                // Fall through to claims / default.
            }
        }

        var user = context.User;
        if (user.Identity?.IsAuthenticated == true)
        {
            var id = user.FindFirst("sub")?.Value ?? user.FindFirst("oid")?.Value;
            if (!string.IsNullOrWhiteSpace(id))
            {
                var permissions = user.FindAll("role").Select(c => c.Value);
                return new Actor(id, permissions);
            }
        }

        return Actor.DefaultAdmin();
    }

    private sealed record TestActorPayload(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("permissions")] string[]? Permissions);
}
