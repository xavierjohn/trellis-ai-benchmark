namespace OrderManagement.Api.v2026_11_12.Controllers;

using Microsoft.AspNetCore.Mvc;
using Trellis.Authorization;
using IResult = Microsoft.AspNetCore.Http.IResult;

[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    protected static IResult ToHttp<T>(Result<T> result, Func<T, object> body, int successStatus = StatusCodes.Status200OK, string? location = null)
    {
        if (result.TryGetValue(out var value))
        {
            return successStatus == StatusCodes.Status201Created
                ? Results.Created(location ?? string.Empty, body(value))
                : Results.Json(body(value), statusCode: successStatus);
        }

        return ToProblem(result.Error!);
    }

    protected static IResult ToProblem(Error error)
    {
        var status = error switch
        {
            Error.InvalidInput => StatusCodes.Status400BadRequest,
            Error.NotFound => StatusCodes.Status404NotFound,
            Error.Conflict => StatusCodes.Status409Conflict,
            Error.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError,
        };

        return Results.Problem(
            title: error.GetType().Name,
            detail: error.Detail,
            statusCode: status,
            extensions: new Dictionary<string, object?> { ["errorType"] = error.GetType().Name });
    }

    protected static async Task<Result<Actor>> RequireActorAsync(IActorProvider actorProvider, string permission, CancellationToken cancellationToken)
    {
        var maybeActor = await actorProvider.GetCurrentActorAsync(cancellationToken);
        if (!maybeActor.TryGetValue(out var actor))
            return Result.Fail<Actor>(new Error.Forbidden("authentication.required", null) { Detail = "Authentication is required." });

        return actor.HasPermission(permission)
            ? Result.Ok(actor)
            : Result.Fail<Actor>(new Error.Forbidden("permission.missing", null) { Detail = $"Missing required permission '{permission}'." });
    }
}
