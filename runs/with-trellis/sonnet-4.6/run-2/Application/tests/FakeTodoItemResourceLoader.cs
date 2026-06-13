namespace Application.Tests;

using OrderManagement.Application.Orders;
using OrderManagement.Domain;
using Trellis.Authorization;
using Trellis.Testing;

/// <summary>
/// Shared resource loader for Order authorization in tests.
/// </summary>
internal sealed class FakeOrderResourceLoader : SharedResourceLoaderById<Order, OrderId>
{
    private readonly FakeRepository<Order, OrderId> _repo;

    public FakeOrderResourceLoader(FakeRepository<Order, OrderId> repo) => _repo = repo;

    public override Task<Result<Order>> GetByIdAsync(OrderId id, CancellationToken cancellationToken) =>
        _repo.GetByIdAsync(id, cancellationToken);
}
