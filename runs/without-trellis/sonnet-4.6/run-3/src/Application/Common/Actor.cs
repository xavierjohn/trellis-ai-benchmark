namespace Application.Common;

public class Actor
{
    public string Id { get; }
    public IReadOnlyList<string> Permissions { get; }

    public Actor(string id, IEnumerable<string> permissions)
    {
        Id = id;
        Permissions = permissions.ToList().AsReadOnly();
    }

    public bool HasPermission(string permission) => Permissions.Contains(permission);

    public static Actor Admin() => new("admin", new[]
    {
        "customers:create",
        "products:create",
        "products:manage-stock",
        "orders:create",
        "orders:submit",
        "orders:approve",
        "orders:ship",
        "orders:deliver",
        "orders:cancel",
        "orders:read",
        "orders:read-all"
    });
}
