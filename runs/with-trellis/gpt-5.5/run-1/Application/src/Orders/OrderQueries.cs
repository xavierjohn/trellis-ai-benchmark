namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Application.Customers;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>
/// Gets an order by id.
/// </summary>
public sealed record GetOrderByIdQuery(OrderId OrderId) : IQuery<Result<Order>>, IAuthorize
{
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersRead];
}

/// <summary>
/// Lists orders belonging to a customer.
/// </summary>
public sealed record ListOrdersByCustomerQuery(CustomerId CustomerId) : IQuery<Result<IReadOnlyList<Order>>>, IAuthorize
{
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersReadAll];
}

/// <summary>
/// Lists submitted orders older than seven days.
/// </summary>
public sealed record ListOverdueOrdersQuery : IQuery<Result<IReadOnlyList<Order>>>, IAuthorize
{
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersReadAll];
}

/// <summary>
/// Handles order lookup.
/// </summary>
public sealed class GetOrderByIdQueryHandler(IOrderRepository repository) : IQueryHandler<GetOrderByIdQuery, Result<Order>>
{
    public async ValueTask<Result<Order>> Handle(GetOrderByIdQuery query, CancellationToken cancellationToken) =>
        await repository.FindByIdAsync(query.OrderId, cancellationToken)
            .ToResultAsync(new Error.NotFound(ResourceRef.For<Order>(query.OrderId)) { Detail = $"Order {query.OrderId} not found." });
}

/// <summary>
/// Handles customer order listing.
/// </summary>
public sealed class ListOrdersByCustomerQueryHandler(ICustomerRepository customerRepository, IOrderRepository orderRepository)
    : IQueryHandler<ListOrdersByCustomerQuery, Result<IReadOnlyList<Order>>>
{
    public async ValueTask<Result<IReadOnlyList<Order>>> Handle(ListOrdersByCustomerQuery query, CancellationToken cancellationToken)
    {
        var customer = await customerRepository.FindByIdAsync(query.CustomerId, cancellationToken);
        if (customer.HasNoValue)
            return Result.Fail<IReadOnlyList<Order>>(new Error.NotFound(ResourceRef.For<Customer>(query.CustomerId)) { Detail = $"Customer {query.CustomerId} not found." });

        var orders = await orderRepository.ListByCustomerAsync(query.CustomerId, cancellationToken);
        return Result.Ok(orders);
    }
}

/// <summary>
/// Handles overdue order listing.
/// </summary>
public sealed class ListOverdueOrdersQueryHandler(IOrderRepository repository, TimeProvider timeProvider)
    : IQueryHandler<ListOverdueOrdersQuery, Result<IReadOnlyList<Order>>>
{
    public async ValueTask<Result<IReadOnlyList<Order>>> Handle(ListOverdueOrdersQuery query, CancellationToken cancellationToken)
    {
        var cutoff = timeProvider.GetUtcNow().UtcDateTime.AddDays(-7);
        var orders = await repository.ListOverdueAsync(cutoff, cancellationToken);
        return Result.Ok(orders);
    }
}
