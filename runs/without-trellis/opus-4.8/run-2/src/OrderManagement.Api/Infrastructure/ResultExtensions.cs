using OrderManagement.Domain.Common;

namespace OrderManagement.Api.Infrastructure;

public static class ResultExtensions
{
    public static IResult ToProblem(this Error error) => error.Kind switch
    {
        ErrorKind.Validation when error.Fields is not null =>
            Results.ValidationProblem(
                error.Fields.ToDictionary(kv => kv.Key, kv => kv.Value),
                detail: error.Message,
                statusCode: StatusCodes.Status400BadRequest),
        ErrorKind.Validation =>
            Results.Problem(detail: error.Message, statusCode: StatusCodes.Status400BadRequest, title: "Validation error"),
        ErrorKind.NotFound =>
            Results.Problem(detail: error.Message, statusCode: StatusCodes.Status404NotFound, title: "Not Found"),
        ErrorKind.Conflict =>
            Results.Problem(detail: error.Message, statusCode: StatusCodes.Status409Conflict, title: "Conflict"),
        ErrorKind.Forbidden =>
            Results.Problem(detail: error.Message, statusCode: StatusCodes.Status403Forbidden, title: "Forbidden"),
        _ =>
            Results.Problem(detail: error.Message, statusCode: StatusCodes.Status500InternalServerError),
    };

    /// <summary>Maps a result to 200 OK on success (with the value as body) or a problem response on failure.</summary>
    public static IResult ToOk<T>(this Result<T> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : result.Error!.ToProblem();

    /// <summary>Maps a result to 201 Created with a Location header on success, or a problem response on failure.</summary>
    public static IResult ToCreated<T>(this Result<T> result, Func<T, string> locationFactory) =>
        result.IsSuccess ? Results.Created(locationFactory(result.Value), result.Value) : result.Error!.ToProblem();
}
