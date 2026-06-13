using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Domain.Exceptions;

namespace OrderManagement.Api.Middleware;

public static class ExceptionHandlerExtensions
{
    public static IApplicationBuilder UseOrderManagementExceptionHandler(
        this IApplicationBuilder app)
    {
        app.UseExceptionHandler(errApp =>
        {
            errApp.Run(async context =>
            {
                var feature = context.Features.Get<IExceptionHandlerFeature>();
                var ex = feature?.Error;
                if (ex is null) return;

                var (status, title, detail) = ex switch
                {
                    ValidationException ve => (
                        StatusCodes.Status400BadRequest,
                        "Validation Error",
                        ve.Errors.Count == 1 ? ve.Errors[0] : string.Join("; ", ve.Errors)),
                    NotFoundException ne => (
                        StatusCodes.Status404NotFound,
                        "Not Found",
                        ne.Message),
                    ConflictException ce => (
                        StatusCodes.Status409Conflict,
                        "Conflict",
                        ce.Message),
                    ForbiddenException fe => (
                        StatusCodes.Status403Forbidden,
                        "Forbidden",
                        fe.Message),
                    _ => (
                        StatusCodes.Status500InternalServerError,
                        "Internal Server Error",
                        "An unexpected error occurred.")
                };

                context.Response.StatusCode = status;
                context.Response.ContentType = "application/problem+json";

                var problem = new ProblemDetails
                {
                    Status = status,
                    Title = title,
                    Detail = detail,
                    Type = $"https://tools.ietf.org/html/rfc9110#section-15.5.{status - 399}"
                };

                if (ex is ValidationException valEx && valEx.Errors.Count > 1)
                    problem.Extensions["errors"] = valEx.Errors;

                await context.Response.WriteAsJsonAsync(problem);
            });
        });

        return app;
    }
}
