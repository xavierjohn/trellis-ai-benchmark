namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Application.Abstractions;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>Lists orders that have been in Submitted status for more than 7 days.</summary>
public sealed record ListOverdueOrdersQuery : IQuery<Result<IReadOnlyList<Order>>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions => [Permissions.OrdersReadAll];
}

/// <summary>Handles <see cref="ListOverdueOrdersQuery"/>.</summary>
public sealed class ListOverdueOrdersQueryHandler(IOrderRepository orderRepository, TimeProvider timeProvider)
    : IQueryHandler<ListOverdueOrdersQuery, Result<IReadOnlyList<Order>>>
{
    /// <inheritdoc />
    public async ValueTask<Result<IReadOnlyList<Order>>> Handle(
        ListOverdueOrdersQuery query,
        CancellationToken cancellationToken)
    {
        var asOf = timeProvider.GetUtcNow().UtcDateTime;
        var orders = await orderRepository.ListOverdueAsync(asOf, cancellationToken);
        return Result.Ok(orders);
    }
}
