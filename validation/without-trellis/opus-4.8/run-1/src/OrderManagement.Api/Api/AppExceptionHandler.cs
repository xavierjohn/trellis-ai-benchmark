using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Api.Domain.Common;

namespace OrderManagement.Api.Api;

/// <summary>Translates domain/application exceptions into RFC 9457 Problem Details responses.</summary>
public sealed class AppExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;

    public AppExceptionHandler(IProblemDetailsService problemDetails) => _problemDetails = problemDetails;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, type) = Map(exception);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Type = type,
            Detail = exception.Message,
            Instance = httpContext.Request.Path
        };

        if (exception is ValidationAppException { Errors.Count: > 0 } v)
            problem.Extensions["errors"] = v.Errors;

        if (exception is AppException app)
            problem.Extensions["errorCode"] = app.ErrorCode;

        httpContext.Response.StatusCode = status;

        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }

    private static (int Status, string Title, string Type) Map(Exception exception) => exception switch
    {
        ValidationAppException => (
            StatusCodes.Status422UnprocessableEntity,
            "Unprocessable Content",
            "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.21"),
        NotFoundAppException => (
            StatusCodes.Status404NotFound,
            "Not Found",
            "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.5"),
        ConflictAppException => (
            StatusCodes.Status409Conflict,
            "Conflict",
            "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.10"),
        ForbiddenAppException => (
            StatusCodes.Status403Forbidden,
            "Forbidden",
            "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.4"),
        BadHttpRequestException => (
            StatusCodes.Status400BadRequest,
            "Bad Request",
            "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.1"),
        _ => (
            StatusCodes.Status500InternalServerError,
            "Internal Server Error",
            "https://datatracker.ietf.org/doc/html/rfc9110#section-15.6.1")
    };
}
