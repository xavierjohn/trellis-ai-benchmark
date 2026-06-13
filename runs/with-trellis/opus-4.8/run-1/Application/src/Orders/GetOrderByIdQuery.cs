namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Application.Abstractions;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>Gets an order by ID.</summary>
public sealed record GetOrderByIdQuery(OrderId OrderId) : IQuery<Result<Order>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions => [Permissions.OrdersRead];
}

/// <summary>Handles <see cref="GetOrderByIdQuery"/>.</summary>
public sealed class GetOrderByIdQueryHandler(IOrderRepository orderRepository)
    : IQueryHandler<GetOrderByIdQuery, Result<Order>>
{
    /// <inheritdoc />
    public async ValueTask<Result<Order>> Handle(GetOrderByIdQuery query, CancellationToken cancellationToken)
    {
        var maybe = await orderRepository.FindByIdAsync(query.OrderId, cancellationToken);
        return maybe.ToResult(new Error.NotFound(ResourceRef.For<Order>(query.OrderId.Value))
        {
            Detail = "Order not found.",
        });
    }
}
