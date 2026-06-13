namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Application.Abstractions;
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
public sealed class ListOrdersByCustomerQueryHandler(
    ICustomerRepository customerRepository,
    IOrderRepository orderRepository)
    : IQueryHandler<ListOrdersByCustomerQuery, Result<IReadOnlyList<Order>>>
{
    /// <inheritdoc />
    public async ValueTask<Result<IReadOnlyList<Order>>> Handle(
        ListOrdersByCustomerQuery query,
        CancellationToken cancellationToken)
    {
        var customerMaybe = await customerRepository.FindByIdAsync(query.CustomerId, cancellationToken);
        if (!customerMaybe.TryGetValue(out _))
            return Result.Fail<IReadOnlyList<Order>>(
                new Error.NotFound(ResourceRef.For<Customer>(query.CustomerId.Value))
                {
                    Detail = "Customer not found.",
                });

        var orders = await orderRepository.ListByCustomerAsync(query.CustomerId, cancellationToken);
        return Result.Ok(orders);
    }
}
