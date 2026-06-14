namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>Lists every order belonging to a customer.</summary>
public sealed record ListOrdersByCustomerQuery(CustomerId CustomerId)
    : IQuery<Result<IReadOnlyList<Order>>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersReadAll];
}

/// <summary>Handler for <see cref="ListOrdersByCustomerQuery"/>.</summary>
public sealed class ListOrdersByCustomerQueryHandler
    : IQueryHandler<ListOrdersByCustomerQuery, Result<IReadOnlyList<Order>>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;

    /// <summary>Creates the handler.</summary>
    public ListOrdersByCustomerQueryHandler(
        IOrderRepository orderRepository, ICustomerRepository customerRepository)
    {
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
    }

    /// <inheritdoc />
    public async ValueTask<Result<IReadOnlyList<Order>>> Handle(
        ListOrdersByCustomerQuery query, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.FindByIdAsync(query.CustomerId, cancellationToken);
        if (!customer.HasValue)
            return Result.Fail<IReadOnlyList<Order>>(new Error.NotFound(ResourceRef.For<Customer>(query.CustomerId))
            {
                Detail = $"Customer {query.CustomerId.Value} not found.",
            });

        var orders = await _orderRepository.ListByCustomerAsync(query.CustomerId, cancellationToken);
        return Result.Ok(orders);
    }
}
