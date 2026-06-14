namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>Get order query.</summary>
public sealed record GetOrderByIdQuery(OrderId OrderId) : IQuery<Result<Order>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersRead];
}

/// <summary>List orders by customer query.</summary>
public sealed record ListOrdersByCustomerQuery(CustomerId CustomerId) : IQuery<Result<IReadOnlyList<Order>>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersReadAll];
}

/// <summary>List overdue orders query.</summary>
public sealed record ListOverdueOrdersQuery : IQuery<Result<IReadOnlyList<Order>>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersReadAll];
}

/// <summary>Get order handler.</summary>
public sealed class GetOrderByIdQueryHandler(IOrderRepository orders)
    : IQueryHandler<GetOrderByIdQuery, Result<Order>>
{
    /// <inheritdoc />
    public async ValueTask<Result<Order>> Handle(GetOrderByIdQuery query, CancellationToken cancellationToken) =>
        await orders.FindByIdAsync(query.OrderId, cancellationToken)
            .ToResultAsync(new Error.NotFound(ResourceRef.For<Order>(query.OrderId)) { Detail = $"Order {query.OrderId.Value} not found." });
}

/// <summary>List by customer handler.</summary>
public sealed class ListOrdersByCustomerQueryHandler(ICustomerRepository customers, IOrderRepository orders)
    : IQueryHandler<ListOrdersByCustomerQuery, Result<IReadOnlyList<Order>>>
{
    /// <inheritdoc />
    public async ValueTask<Result<IReadOnlyList<Order>>> Handle(ListOrdersByCustomerQuery query, CancellationToken cancellationToken)
    {
        var customer = await customers.FindByIdAsync(query.CustomerId, cancellationToken);
        if (customer.HasNoValue)
            return Result.Fail<IReadOnlyList<Order>>(new Error.NotFound(ResourceRef.For<Customer>(query.CustomerId)) { Detail = $"Customer {query.CustomerId.Value} not found." });

        return Result.Ok(await orders.ListByCustomerAsync(query.CustomerId, cancellationToken));
    }
}

/// <summary>Overdue orders handler.</summary>
public sealed class ListOverdueOrdersQueryHandler(IOrderRepository orders, TimeProvider timeProvider)
    : IQueryHandler<ListOverdueOrdersQuery, Result<IReadOnlyList<Order>>>
{
    /// <inheritdoc />
    public async ValueTask<Result<IReadOnlyList<Order>>> Handle(ListOverdueOrdersQuery query, CancellationToken cancellationToken) =>
        Result.Ok(await orders.ListOverdueAsync(timeProvider.GetUtcNow(), cancellationToken));
}
