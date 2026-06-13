namespace OrderManagement.Application;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>Lists all orders belonging to a customer.</summary>
public sealed record ListOrdersByCustomerQuery(CustomerId CustomerId)
    : IQuery<Result<IReadOnlyList<Order>>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions => [Permissions.OrdersReadAll];
}

/// <summary>Handles <see cref="ListOrdersByCustomerQuery"/>.</summary>
public sealed class ListOrdersByCustomerQueryHandler(ICustomerRepository customers, IOrderRepository orders)
    : IQueryHandler<ListOrdersByCustomerQuery, Result<IReadOnlyList<Order>>>
{
    /// <inheritdoc />
    public async ValueTask<Result<IReadOnlyList<Order>>> Handle(ListOrdersByCustomerQuery query, CancellationToken cancellationToken)
    {
        var customerExists = await customers.ExistsAsync(query.CustomerId, cancellationToken);
        if (!customerExists)
            return Result.Fail<IReadOnlyList<Order>>(new Error.NotFound(ResourceRef.For<Customer>(query.CustomerId)) { Detail = "Customer not found." });

        var result = await orders.QueryAsync(new OrdersByCustomerSpecification(query.CustomerId), cancellationToken);
        return Result.Ok(result);
    }
}
