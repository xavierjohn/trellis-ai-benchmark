namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Domain;
using Trellis;
using Trellis.Authorization;

/// <summary>Lists overdue orders.</summary>
public sealed record ListOverdueOrdersQuery : IQuery<Result<IReadOnlyList<Order>>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersReadAll];
}

/// <summary>Handler for <see cref="ListOverdueOrdersQuery"/>.</summary>
internal sealed class ListOverdueOrdersQueryHandler(
    IOrderRepository orderRepository,
    TimeProvider timeProvider) : IQueryHandler<ListOverdueOrdersQuery, Result<IReadOnlyList<Order>>>
{
    public async ValueTask<Result<IReadOnlyList<Order>>> Handle(ListOverdueOrdersQuery query, CancellationToken cancellationToken)
    {
        var orders = await orderRepository.QueryAsync(new OverdueOrderSpecification(timeProvider), cancellationToken);
        return Result.Ok(orders);
    }
}
