namespace OrderManagement.Application.Abstractions;

/// <summary>The authenticated principal performing an operation.</summary>
public sealed class Actor
{
    public Actor(string id, IEnumerable<string> permissions)
    {
        Id = id;
        Permissions = new HashSet<string>(permissions, StringComparer.OrdinalIgnoreCase);
    }

    public string Id { get; }
    public IReadOnlySet<string> Permissions { get; }

    public bool Has(string permission) => Permissions.Contains(permission);

    /// <summary>An actor holding every permission. Used as the default when no actor header is present.</summary>
    public static Actor Admin(string id = "admin") => new(id, Application.Abstractions.Permissions.All);
}

/// <summary>Supplies the current <see cref="Actor"/> for the active request.</summary>
public interface IActorProvider
{
    Actor GetCurrentActor();
}
