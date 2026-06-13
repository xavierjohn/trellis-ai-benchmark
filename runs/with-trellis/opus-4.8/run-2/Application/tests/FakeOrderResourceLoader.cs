namespace Application.Tests;

using OrderManagement.Domain;
using Trellis.Authorization;
using Trellis.Testing;

/// <summary>Shared resource loader for <see cref="Order"/> authorization in tests.</summary>
internal sealed class FakeOrderResourceLoader(FakeRepository<Order, OrderId> repo)
    : SharedResourceLoaderById<Order, OrderId>
{
    public override Task<Result<Order>> GetByIdAsync(OrderId id, CancellationToken cancellationToken) =>
        repo.GetByIdAsync(id, cancellationToken);
}
