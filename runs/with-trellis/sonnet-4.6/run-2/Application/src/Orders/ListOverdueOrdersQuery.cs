namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>Lists all overdue orders (submitted more than 7 days ago).</summary>
public sealed record ListOverdueOrdersQuery : IQuery<Result<IReadOnlyList<Order>>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersReadAll];
}

/// <summary>Handler for ListOverdueOrdersQuery.</summary>
public sealed class ListOverdueOrdersQueryHandler : IQueryHandler<ListOverdueOrdersQuery, Result<IReadOnlyList<Order>>>
{
    private readonly IOrderRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ListOverdueOrdersQueryHandler(IOrderRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async ValueTask<Result<IReadOnlyList<Order>>> Handle(ListOverdueOrdersQuery query, CancellationToken cancellationToken)
    {
        var orders = await _repository.QueryAsync(new OverdueOrderSpecification(_timeProvider), cancellationToken);
        return Result.Ok(orders);
    }
}
