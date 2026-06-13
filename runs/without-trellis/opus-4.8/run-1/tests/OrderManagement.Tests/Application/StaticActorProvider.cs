using OrderManagement.Application.Authorization;

namespace OrderManagement.Tests.Application;

/// <summary>Test actor provider returning a fixed actor.</summary>
internal sealed class StaticActorProvider : IActorProvider
{
    public StaticActorProvider(string id, params string[] permissions)
        => Current = new Actor(id, permissions);

    public IActor Current { get; }

    public static StaticActorProvider WithAll(string id = "admin-1")
        => new(id, Permissions.All.ToArray());
}
