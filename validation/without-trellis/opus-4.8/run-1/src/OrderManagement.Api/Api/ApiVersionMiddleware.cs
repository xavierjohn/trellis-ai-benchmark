using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace OrderManagement.Api.Api;

/// <summary>
/// Enforces the presence of the <c>api-version</c> query parameter on all /api/* routes.
/// A missing version is a malformed request and returns 400 Bad Request (spec §7, §9).
/// </summary>
public sealed class ApiVersionMiddleware
{
    public const string VersionQueryKey = "api-version";

    private readonly RequestDelegate _next;

    public ApiVersionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IProblemDetailsService problemDetails)
    {
        var path = context.Request.Path;

        if (path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            var version = context.Request.Query[VersionQueryKey];
            if (string.IsNullOrWhiteSpace(version))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await problemDetails.TryWriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context,
                    ProblemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Bad Request",
                        Type = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.1",
                        Detail = $"The required '{VersionQueryKey}' query parameter is missing.",
                        Instance = path,
                        Extensions = { ["errorCode"] = "missing_api_version" }
                    }
                });
                return;
            }
        }

        await _next(context);
    }
}
