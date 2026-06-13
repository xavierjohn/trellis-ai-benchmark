namespace OrderManagement.Application;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>Lists overdue orders (still Submitted more than 7 days after submission).</summary>
public sealed record ListOverdueOrdersQuery : IQuery<Result<IReadOnlyList<Order>>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions => [Permissions.OrdersReadAll];
}

/// <summary>Handles <see cref="ListOverdueOrdersQuery"/>.</summary>
public sealed class ListOverdueOrdersQueryHandler(IOrderRepository orders, TimeProvider timeProvider)
    : IQueryHandler<ListOverdueOrdersQuery, Result<IReadOnlyList<Order>>>
{
    /// <inheritdoc />
    public async ValueTask<Result<IReadOnlyList<Order>>> Handle(ListOverdueOrdersQuery query, CancellationToken cancellationToken)
    {
        var specification = new OverdueOrderSpecification(timeProvider.GetUtcNow().UtcDateTime);
        var result = await orders.QueryAsync(specification, cancellationToken);
        return Result.Ok(result);
    }
}
