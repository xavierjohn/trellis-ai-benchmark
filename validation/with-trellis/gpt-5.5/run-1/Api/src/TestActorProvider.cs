namespace OrderManagement.Api;

using System.Text.Json;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>Reads actors from X-Test-Actor and falls back to an admin actor.</summary>
public sealed class TestHeaderActorProvider(IHttpContextAccessor httpContextAccessor) : IActorProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private sealed record HeaderActor(string Id, string[] Permissions);

    /// <inheritdoc />
    public Task<Maybe<Actor>> GetCurrentActorAsync(CancellationToken cancellationToken = default)
    {
        var context = httpContextAccessor.HttpContext;
        if (context is null)
            return Task.FromResult(Maybe.From(Actor.Create("admin", Permissions.All)));

        if (!context.Request.Headers.TryGetValue("X-Test-Actor", out var values) || string.IsNullOrWhiteSpace(values.ToString()))
            return Task.FromResult(Maybe.From(Actor.Create("admin", Permissions.All)));

        var actor = JsonSerializer.Deserialize<HeaderActor>(values.ToString(), JsonOptions);
        if (actor is null || string.IsNullOrWhiteSpace(actor.Id))
            return Task.FromResult(Maybe.From(Actor.Create("admin", Permissions.All)));

        return Task.FromResult(Maybe.From(Actor.Create(actor.Id, actor.Permissions.ToHashSet(StringComparer.Ordinal))));
    }
}
