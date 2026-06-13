using OrderManagement.Application.Authorization;
using OrderManagement.Domain.Common;

namespace OrderManagement.Application.Authorization;

/// <summary>Helpers for permission checks producing <see cref="Error"/> on failure.</summary>
public static class AuthorizationGuard
{
    public static Result Require(IActor actor, string permission)
        => actor.Has(permission)
            ? Result.Success()
            : Error.Forbidden($"Actor '{actor.Id}' is missing the required permission '{permission}'.");
}
