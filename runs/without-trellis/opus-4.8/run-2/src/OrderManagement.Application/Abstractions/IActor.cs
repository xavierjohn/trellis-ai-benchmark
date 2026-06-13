namespace OrderManagement.Application.Abstractions;

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

    public static Actor Admin(string id = "admin") => new(id, Roles.Admin);
}
