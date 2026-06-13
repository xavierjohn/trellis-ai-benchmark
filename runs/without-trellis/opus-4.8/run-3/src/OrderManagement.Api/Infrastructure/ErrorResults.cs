using Microsoft.AspNetCore.Http.HttpResults;
using OrderManagement.Domain.Common;

namespace OrderManagement.Api.Infrastructure;

/// <summary>Maps domain/application <see cref="Error"/> values to RFC 9457 Problem Details responses.</summary>
public static class ErrorResults
{
    public static IResult ToProblem(this Error error)
    {
        return error.Type switch
        {
            ErrorType.Validation => ValidationProblem(error),
            ErrorType.NotFound => Problem(error, StatusCodes.Status404NotFound, "Not Found"),
            ErrorType.Conflict => Problem(error, StatusCodes.Status409Conflict, "Conflict"),
            ErrorType.Forbidden => Problem(error, StatusCodes.Status403Forbidden, "Forbidden"),
            _ => Problem(error, StatusCodes.Status500InternalServerError, "Server Error")
        };
    }

    private static IResult ValidationProblem(Error error)
    {
        if (error.FieldErrors is { Count: > 0 })
        {
            return Results.ValidationProblem(
                errors: error.FieldErrors.ToDictionary(kv => kv.Key, kv => kv.Value),
                detail: error.Message,
                title: "Validation Failed",
                extensions: new Dictionary<string, object?> { ["code"] = error.Code });
        }

        return Results.Problem(
            detail: error.Message,
            statusCode: StatusCodes.Status400BadRequest,
            title: "Validation Failed",
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }

    private static IResult Problem(Error error, int statusCode, string title)
        => Results.Problem(
            detail: error.Message,
            statusCode: statusCode,
            title: title,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
}
