using Microsoft.AspNetCore.Mvc;

namespace OrderManagement.Api.Endpoints;

public static class ApiVersionExtensions
{
    private const string RequiredVersion = "2026-11-12";

    public static RouteHandlerBuilder RequiresApiVersion(this RouteHandlerBuilder builder) =>
        builder.AddEndpointFilter(async (ctx, next) =>
        {
            var version = ctx.HttpContext.Request.Query["api-version"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(version))
            {
                var problem = new ProblemDetails
                {
                    Status = 400,
                    Title = "Bad Request",
                    Detail = "The 'api-version' query parameter is required.",
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1"
                };
                return Results.Json(problem, statusCode: 400, contentType: "application/problem+json");
            }
            return await next(ctx);
        });
}
