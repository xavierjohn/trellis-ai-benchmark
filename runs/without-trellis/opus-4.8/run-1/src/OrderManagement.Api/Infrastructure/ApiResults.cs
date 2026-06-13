using OrderManagement.Domain.Common;

namespace OrderManagement.Api.Infrastructure;

/// <summary>Maps domain <see cref="Error"/> values and <see cref="Result{T}"/> outcomes to HTTP results (RFC 9457).</summary>
public static class ApiResults
{
    public static IResult ToProblem(Error error)
    {
        return error.Kind switch
        {
            ErrorKind.Validation => Results.ValidationProblem(
                error.Fields ?? new Dictionary<string, string[]> { ["request"] = new[] { error.Message } },
                detail: error.Message,
                title: "Validation failed",
                type: "https://datatracker.ietf.org/doc/html/rfc9457",
                extensions: new Dictionary<string, object?> { ["code"] = error.Code }),

            ErrorKind.NotFound => Problem(error, StatusCodes.Status404NotFound, "Resource not found"),
            ErrorKind.Conflict => Problem(error, StatusCodes.Status409Conflict, "Conflict"),
            ErrorKind.Forbidden => Problem(error, StatusCodes.Status403Forbidden, "Forbidden"),
            _ => Problem(error, StatusCodes.Status500InternalServerError, "Unexpected error")
        };
    }

    private static IResult Problem(Error error, int statusCode, string title) =>
        Results.Problem(
            detail: error.Message,
            statusCode: statusCode,
            title: title,
            type: "https://datatracker.ietf.org/doc/html/rfc9457",
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });

    public static IResult Ok<T>(Result<T> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : ToProblem(result.Error!);

    public static IResult Created<T>(Result<T> result, Func<T, string> locationFactory) =>
        result.IsSuccess
            ? Results.Created(locationFactory(result.Value), result.Value)
            : ToProblem(result.Error!);
}
