using System.Text.Json;
using OrderManagement.Api.Auth;

namespace OrderManagement.Api.Auth;

public static class ActorExtensions
{
    private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);

    public static Actor GetActor(this HttpContext context)
    {
        if (context.Items.TryGetValue("CurrentActor", out var cached) && cached is Actor actor)
            return actor;

        return Actor.DefaultAdmin;
    }
}

public class ActorMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);

    public ActorMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var header = context.Request.Headers["X-Test-Actor"].FirstOrDefault();
        if (header is not null)
        {
            try
            {
                var actor = JsonSerializer.Deserialize<ActorDto>(header, _options);
                if (actor is not null)
                {
                    context.Items["CurrentActor"] = new Actor(actor.Id, actor.Permissions);
                }
                else
                {
                    context.Items["CurrentActor"] = Actor.DefaultAdmin;
                }
            }
            catch
            {
                context.Items["CurrentActor"] = Actor.DefaultAdmin;
            }
        }
        else
        {
            context.Items["CurrentActor"] = Actor.DefaultAdmin;
        }

        await _next(context);
    }

    private record ActorDto(string Id, string[] Permissions);
}
