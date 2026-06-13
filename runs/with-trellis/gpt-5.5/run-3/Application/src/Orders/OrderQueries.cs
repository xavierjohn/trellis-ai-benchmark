namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;

public sealed record GetOrderByIdQuery(OrderId OrderId) : IQuery<Result<Order>>, IAuthorize
{
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersRead];
}

public sealed record ListOrdersByCustomerQuery(CustomerId CustomerId) : IQuery<Result<IReadOnlyList<Order>>>, IAuthorize
{
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersReadAll];
}

public sealed record ListOverdueOrdersQuery : IQuery<Result<IReadOnlyList<Order>>>, IAuthorize
{
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersReadAll];
}

public sealed class GetOrderByIdQueryHandler : IQueryHandler<GetOrderByIdQuery, Result<Order>>
{
    private readonly IOrderRepository _orders;

    public GetOrderByIdQueryHandler(IOrderRepository orders) => _orders = orders;

    public async ValueTask<Result<Order>> Handle(GetOrderByIdQuery query, CancellationToken cancellationToken) =>
        (await _orders.FindByIdAsync(query.OrderId, cancellationToken)).ToResult(NotFound.Order(query.OrderId));
}

public sealed class ListOrdersByCustomerQueryHandler : IQueryHandler<ListOrdersByCustomerQuery, Result<IReadOnlyList<Order>>>
{
    private readonly ICustomerRepository _customers;
    private readonly IOrderRepository _orders;

    public ListOrdersByCustomerQueryHandler(ICustomerRepository customers, IOrderRepository orders)
    {
        _customers = customers;
        _orders = orders;
    }

    public async ValueTask<Result<IReadOnlyList<Order>>> Handle(ListOrdersByCustomerQuery query, CancellationToken cancellationToken)
    {
        var customer = await _customers.FindByIdAsync(query.CustomerId, cancellationToken);
        if (customer.HasNoValue)
            return Result.Fail<IReadOnlyList<Order>>(NotFound.Customer(query.CustomerId));

        return Result.Ok(await _orders.ListByCustomerAsync(query.CustomerId, cancellationToken));
    }
}

public sealed class ListOverdueOrdersQueryHandler : IQueryHandler<ListOverdueOrdersQuery, Result<IReadOnlyList<Order>>>
{
    private readonly IOrderRepository _orders;
    private readonly TimeProvider _timeProvider;

    public ListOverdueOrdersQueryHandler(IOrderRepository orders, TimeProvider timeProvider)
    {
        _orders = orders;
        _timeProvider = timeProvider;
    }

    public async ValueTask<Result<IReadOnlyList<Order>>> Handle(ListOverdueOrdersQuery query, CancellationToken cancellationToken) =>
        Result.Ok(await _orders.ListOverdueAsync(_timeProvider.GetUtcNow().UtcDateTime, cancellationToken));
}
