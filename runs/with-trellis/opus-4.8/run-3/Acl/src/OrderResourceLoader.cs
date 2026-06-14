namespace OrderManagement.AntiCorruptionLayer;

using OrderManagement.Application;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>Shared resource loader for <see cref="Order"/> authorization.</summary>
public sealed class OrderResourceLoader : SharedResourceLoaderById<Order, OrderId>
{
    private readonly IOrderRepository _repository;

    /// <summary>Creates the loader.</summary>
    public OrderResourceLoader(IOrderRepository repository) => _repository = repository;

    /// <inheritdoc />
    public override async Task<Result<Order>> GetByIdAsync(OrderId id, CancellationToken cancellationToken) =>
        (await _repository.FindByIdAsync(id, cancellationToken))
            .ToResult(new Error.NotFound(ResourceRef.For<Order>(id)) { Detail = "Order not found." });
}
