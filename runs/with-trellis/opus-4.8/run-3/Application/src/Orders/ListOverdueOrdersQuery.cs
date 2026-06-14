namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>Lists overdue orders (Submitted for more than 7 days without approval).</summary>
public sealed record ListOverdueOrdersQuery : IQuery<Result<IReadOnlyList<Order>>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersReadAll];
}

/// <summary>Handler for <see cref="ListOverdueOrdersQuery"/>.</summary>
public sealed class ListOverdueOrdersQueryHandler
    : IQueryHandler<ListOverdueOrdersQuery, Result<IReadOnlyList<Order>>>
{
    private readonly IOrderRepository _repository;
    private readonly TimeProvider _timeProvider;

    /// <summary>Creates the handler.</summary>
    public ListOverdueOrdersQueryHandler(IOrderRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async ValueTask<Result<IReadOnlyList<Order>>> Handle(
        ListOverdueOrdersQuery query, CancellationToken cancellationToken)
    {
        var spec = new OverdueOrderSpecification(_timeProvider);
        var orders = await _repository.QueryAsync(spec, cancellationToken);
        return Result.Ok(orders);
    }
}
