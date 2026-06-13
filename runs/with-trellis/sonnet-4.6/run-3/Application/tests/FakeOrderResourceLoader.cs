namespace Application.Tests;

using OrderManagement.Domain;
using Trellis;
using Trellis.Authorization;
using Trellis.Testing;

internal sealed class FakeOrderResourceLoader : SharedResourceLoaderById<Order, OrderId>
{
    private readonly FakeRepository<Order, OrderId> _repo;

    public FakeOrderResourceLoader(FakeRepository<Order, OrderId> repo) => _repo = repo;

    public override Task<Result<Order>> GetByIdAsync(OrderId id, CancellationToken cancellationToken) =>
        _repo.GetByIdAsync(id, cancellationToken);
}
