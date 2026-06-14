namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>Gets a single order by ID.</summary>
public sealed record GetOrderByIdQuery(OrderId OrderId) : IQuery<Result<Order>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersRead];
}

/// <summary>Handler for <see cref="GetOrderByIdQuery"/>.</summary>
public sealed class GetOrderByIdQueryHandler : IQueryHandler<GetOrderByIdQuery, Result<Order>>
{
    private readonly IOrderRepository _repository;

    /// <summary>Creates the handler.</summary>
    public GetOrderByIdQueryHandler(IOrderRepository repository) => _repository = repository;

    /// <inheritdoc />
    public async ValueTask<Result<Order>> Handle(GetOrderByIdQuery query, CancellationToken cancellationToken) =>
        await _repository.FindByIdAsync(query.OrderId, cancellationToken)
            .ToResultAsync(new Error.NotFound(ResourceRef.For<Order>(query.OrderId))
            {
                Detail = $"Order {query.OrderId.Value} not found.",
            });
}
