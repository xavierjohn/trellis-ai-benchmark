using System.Text.Json;
using System.Text.Json.Serialization;
using OrderManagement.Application.Abstractions;

namespace OrderManagement.Api.Infrastructure;

public static class ApiConstants
{
    public const string Version = "2026-11-12";
    public const string TestActorHeader = "X-Test-Actor";
}

/// <summary>Resolves the current <see cref="IActor"/> from the request's X-Test-Actor header.</summary>
public static class ActorParser
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private sealed record TestActorPayload(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("permissions")] string[]? Permissions);

    public static IActor Resolve(HttpContext? context)
    {
        if (context is null || !context.Request.Headers.TryGetValue(ApiConstants.TestActorHeader, out var raw))
            return Actor.Admin();

        var json = raw.ToString();
        if (string.IsNullOrWhiteSpace(json))
            return Actor.Admin();

        try
        {
            var payload = JsonSerializer.Deserialize<TestActorPayload>(json, JsonOptions);
            if (payload is null || string.IsNullOrWhiteSpace(payload.Id))
                return Actor.Admin();

            return new Actor(payload.Id, payload.Permissions ?? []);
        }
        catch (JsonException)
        {
            // Malformed header: fall back to the default Admin actor.
            return Actor.Admin();
        }
    }
}
