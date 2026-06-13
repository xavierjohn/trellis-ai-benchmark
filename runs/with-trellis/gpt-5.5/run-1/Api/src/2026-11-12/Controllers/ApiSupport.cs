namespace OrderManagement.Api.v2026_11_12.Controllers;

using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Domain;
using Trellis;

internal sealed record TestActor(string Id, IReadOnlySet<string> Permissions)
{
    public bool Has(string permission) => Permissions.Contains(permission);
}

internal static class ApiSupport
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static TestActor GetActor(HttpRequest request)
    {
        if (!request.Headers.TryGetValue("X-Test-Actor", out var values))
            return new TestActor("admin", Permissions.All.ToHashSet(StringComparer.Ordinal));

        var parsed = JsonSerializer.Deserialize<TestActorPayload>(values.ToString(), JsonOptions);
        if (parsed is null || string.IsNullOrWhiteSpace(parsed.Id))
            return new TestActor("admin", Permissions.All.ToHashSet(StringComparer.Ordinal));

        return new TestActor(parsed.Id, (parsed.Permissions ?? []).ToHashSet(StringComparer.Ordinal));
    }

    public static ActionResult? ForbidUnless(ControllerBase controller, TestActor actor, string permission) =>
        actor.Has(permission)
            ? null
            : controller.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Forbidden",
                detail: "Insufficient permissions.",
                type: "https://httpstatuses.com/403");

    public static ActionResult Problem(ControllerBase controller, Error error)
    {
        var status = error switch
        {
            Error.InvalidInput => StatusCodes.Status400BadRequest,
            Error.NotFound => StatusCodes.Status404NotFound,
            Error.Conflict => StatusCodes.Status409Conflict,
            Error.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError,
        };

        return controller.Problem(
            statusCode: status,
            title: error.GetType().Name,
            detail: error.Detail,
            type: $"https://httpstatuses.com/{status}");
    }

    private sealed record TestActorPayload(string? Id, string[]? Permissions);
}
