using OrderManagement.Api.Domain.Common;

namespace OrderManagement.Api.Application.Auth;

/// <summary>The authenticated user performing an operation.</summary>
public sealed class Actor
{
    private readonly HashSet<string> _permissions;

    public Actor(string id, IEnumerable<string> permissions)
    {
        Id = id;
        _permissions = new HashSet<string>(permissions, StringComparer.OrdinalIgnoreCase);
    }

    public string Id { get; }

    public IReadOnlySet<string> Permissions => _permissions;

    public bool Has(string permission) => _permissions.Contains(permission);

    /// <summary>Throws <see cref="ForbiddenAppException"/> if the permission is missing.</summary>
    public void Require(string permission)
    {
        if (!Has(permission))
            throw new ForbiddenAppException($"Permission '{permission}' is required to perform this operation.");
    }

    /// <summary>A default actor with all permissions, used when no auth context is supplied.</summary>
    public static Actor DefaultAdmin() => new("default-admin", Auth.Permissions.All);
}
