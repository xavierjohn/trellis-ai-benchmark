namespace OrderManagement.Api;

using System.Text.Json;
using Microsoft.Extensions.Primitives;
using OrderManagement.Domain;
using Trellis.Authorization;

internal sealed class TestHeaderActorProvider : IActorProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TestHeaderActorProvider(IHttpContextAccessor httpContextAccessor) => _httpContextAccessor = httpContextAccessor;

    public Task<Maybe<Actor>> GetCurrentActorAsync(CancellationToken cancellationToken = default)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null)
            return Task.FromResult(Maybe.From(Actor.Create("admin", Permissions.All)));

        if (!context.Request.Headers.TryGetValue("X-Test-Actor", out StringValues values) || StringValues.IsNullOrEmpty(values))
            return Task.FromResult(Maybe.From(Actor.Create("admin", Permissions.All)));

        var payload = JsonSerializer.Deserialize<TestActorPayload>(values.ToString(), JsonOptions);
        var id = string.IsNullOrWhiteSpace(payload?.Id) ? "admin" : payload.Id;
        var permissions = payload?.Permissions?.ToHashSet(StringComparer.Ordinal) ?? Permissions.All;
        return Task.FromResult(Maybe.From(Actor.Create(id, permissions)));
    }

    private sealed record TestActorPayload(string? Id, string[]? Permissions);
}
