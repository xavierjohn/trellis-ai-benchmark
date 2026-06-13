namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Domain;
using Trellis;
using Trellis.Authorization;

/// <summary>Gets an order by ID.</summary>
public sealed record GetOrderByIdQuery(OrderId OrderId) : IQuery<Result<Order>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersRead];
}

/// <summary>Handler for <see cref="GetOrderByIdQuery"/>.</summary>
internal sealed class GetOrderByIdQueryHandler(
    IOrderRepository orderRepository) : IQueryHandler<GetOrderByIdQuery, Result<Order>>
{
    public async ValueTask<Result<Order>> Handle(GetOrderByIdQuery query, CancellationToken cancellationToken) =>
        await orderRepository.FindByIdAsync(query.OrderId, cancellationToken)
            .ToResultAsync(new Error.NotFound(ResourceRef.For<Order>(query.OrderId)) { Detail = $"Order {query.OrderId.Value} not found." });
}
