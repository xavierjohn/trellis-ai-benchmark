namespace Application.Tests;

using OrderManagement.Domain;
using Trellis.Authorization;
using Trellis.Testing;

internal sealed class FakeOrderResourceLoader(FakeRepository<Order, OrderId> repository) : SharedResourceLoaderById<Order, OrderId>
{
    public override Task<Result<Order>> GetByIdAsync(OrderId id, CancellationToken cancellationToken) =>
        repository.GetByIdAsync(id, cancellationToken);
}
