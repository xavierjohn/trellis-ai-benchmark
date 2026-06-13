namespace OrderManagement.Application;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>Gets an order by id.</summary>
public sealed record GetOrderByIdQuery(OrderId OrderId) : IQuery<Result<Order>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions => [Permissions.OrdersRead];
}

/// <summary>Handles <see cref="GetOrderByIdQuery"/>.</summary>
public sealed class GetOrderByIdQueryHandler(IOrderRepository orders)
    : IQueryHandler<GetOrderByIdQuery, Result<Order>>
{
    /// <inheritdoc />
    public async ValueTask<Result<Order>> Handle(GetOrderByIdQuery query, CancellationToken cancellationToken)
    {
        var maybeOrder = await orders.FindByIdAsync(query.OrderId, cancellationToken);
        return maybeOrder.ToResult(new Error.NotFound(ResourceRef.For<Order>(query.OrderId)) { Detail = "Order not found." });
    }
}
