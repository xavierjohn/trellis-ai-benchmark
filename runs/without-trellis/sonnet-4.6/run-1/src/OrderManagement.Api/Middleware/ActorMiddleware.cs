using System.Text.Json;
using OrderManagement.Application.Models;

namespace OrderManagement.Api.Middleware;

public class ActorMiddleware(RequestDelegate next)
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerOptions.Default)
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task InvokeAsync(HttpContext context)
    {
        var actorHeader = context.Request.Headers["X-Test-Actor"].FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(actorHeader))
        {
            try
            {
                var dto = JsonSerializer.Deserialize<ActorHeaderDto>(actorHeader, _jsonOptions);
                if (dto is not null)
                {
                    var actor = new Actor
                    {
                        Id = dto.Id ?? "unknown",
                        Permissions = new HashSet<string>(dto.Permissions ?? [])
                    };
                    context.Items["Actor"] = actor;
                }
            }
            catch
            {
                // Fall through to default admin actor
            }
        }

        if (context.Items["Actor"] is null)
            context.Items["Actor"] = Actor.Admin;

        await next(context);
    }

    private class ActorHeaderDto
    {
        public string? Id { get; set; }
        public List<string>? Permissions { get; set; }
    }
}

public static class ActorExtensions
{
    public static Actor GetActor(this HttpContext context) =>
        context.Items["Actor"] as Actor ?? Actor.Admin;
}
