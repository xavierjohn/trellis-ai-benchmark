using OrderManagement.Domain.Common;

namespace OrderManagement.Api.Infrastructure;

public static class ResultExtensions
{
    /// <summary>200 OK with the value, or the mapped problem response on failure.</summary>
    public static IResult ToOk<T>(this Result<T> result)
        => result.IsSuccess ? Results.Ok(result.Value) : result.Error!.ToProblem();
}
