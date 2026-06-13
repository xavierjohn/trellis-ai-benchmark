namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Domain;
using Trellis;
using Trellis.Authorization;

/// <summary>Lists all orders for a customer.</summary>
public sealed record ListOrdersByCustomerQuery(CustomerId CustomerId) : IQuery<Result<IReadOnlyList<Order>>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersReadAll];
}

/// <summary>Handler for <see cref="ListOrdersByCustomerQuery"/>.</summary>
internal sealed class ListOrdersByCustomerQueryHandler(
    IOrderRepository orderRepository,
    ICustomerRepository customerRepository) : IQueryHandler<ListOrdersByCustomerQuery, Result<IReadOnlyList<Order>>>
{
    public async ValueTask<Result<IReadOnlyList<Order>>> Handle(ListOrdersByCustomerQuery query, CancellationToken cancellationToken)
    {
        var customerResult = await customerRepository.FindByIdAsync(query.CustomerId, cancellationToken)
            .ToResultAsync(new Error.NotFound(ResourceRef.For<Customer>(query.CustomerId)) { Detail = $"Customer {query.CustomerId.Value} not found." });
        if (!customerResult.TryGetValue(out _))
            return Result.Fail<IReadOnlyList<Order>>(customerResult.Error!);

        var orders = await orderRepository.FindByCustomerIdAsync(query.CustomerId, cancellationToken);
        return Result.Ok(orders);
    }
}
