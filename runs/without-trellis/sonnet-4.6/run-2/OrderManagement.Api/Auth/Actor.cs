namespace OrderManagement.Api.Auth;

public record Actor(string Id, string[] Permissions)
{
    public static Actor DefaultAdmin { get; } = new Actor("admin", PermissionConstants.All);

    public bool HasPermission(string permission)
        => Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);

    public void RequirePermission(string permission)
    {
        if (!HasPermission(permission))
            throw new Domain.ForbiddenException(
                $"Actor '{Id}' does not have the required permission '{permission}'.");
    }
}
