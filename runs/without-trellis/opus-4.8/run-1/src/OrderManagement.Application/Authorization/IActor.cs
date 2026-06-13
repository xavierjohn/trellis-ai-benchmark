namespace OrderManagement.Application.Authorization;

/// <summary>The authenticated user performing an operation.</summary>
public interface IActor
{
    string Id { get; }
    IReadOnlySet<string> Permissions { get; }
    bool Has(string permission);
}

public sealed class Actor : IActor
{
    public string Id { get; }
    public IReadOnlySet<string> Permissions { get; }

    public Actor(string id, IEnumerable<string> permissions)
    {
        Id = id;
        Permissions = new HashSet<string>(permissions, StringComparer.OrdinalIgnoreCase);
    }

    public bool Has(string permission) => Permissions.Contains(permission);

    /// <summary>A default actor that holds every permission. Used when no actor context is supplied.</summary>
    public static Actor DefaultAdmin() => new("default-admin", Authorization.Permissions.All);
}

/// <summary>Supplies the current actor to handlers.</summary>
public interface IActorProvider
{
    IActor Current { get; }
}
