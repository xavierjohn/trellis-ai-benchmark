using System.Text.Json;
using OrderManagement.Api.Application.Auth;

namespace OrderManagement.Api.Api;

/// <summary>
/// Resolves the current actor from the request. For testing/evaluation it reads the
/// <c>X-Test-Actor</c> header (JSON: {"id": "...", "permissions": [...]}). When absent,
/// it falls back to claims (sub/oid + role) and finally to a default Admin actor.
/// </summary>
public sealed class HttpActorProvider : IActorProvider
{
    public const string TestActorHeader = "X-Test-Actor";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IHttpContextAccessor _accessor;

    public HttpActorProvider(IHttpContextAccessor accessor) => _accessor = accessor;

    public Actor GetCurrentActor()
    {
        var context = _accessor.HttpContext;
        if (context is null)
            return Actor.DefaultAdmin();

        if (context.Request.Headers.TryGetValue(TestActorHeader, out var raw) && !string.IsNullOrWhiteSpace(raw))
        {
            var actor = TryParseTestActor(raw!);
            if (actor is not null)
                return actor;
        }

        var user = context.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            var id = user.FindFirst("sub")?.Value
                     ?? user.FindFirst("oid")?.Value
                     ?? user.Identity.Name
                     ?? "unknown";
            var permissions = user.FindAll("role").Select(c => c.Value).ToList();
            return new Actor(id, permissions);
        }

        return Actor.DefaultAdmin();
    }

    private static Actor? TryParseTestActor(string raw)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<TestActorPayload>(raw, JsonOptions);
            if (payload is null || string.IsNullOrWhiteSpace(payload.Id))
                return null;

            return new Actor(payload.Id, payload.Permissions ?? []);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record TestActorPayload(string? Id, List<string>? Permissions);
}
