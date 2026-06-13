namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Application.Customers;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>Lists all orders for a specific customer.</summary>
public sealed record ListOrdersByCustomerQuery(CustomerId CustomerId) : IQuery<Result<IReadOnlyList<Order>>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersReadAll];
}

/// <summary>Handler for ListOrdersByCustomerQuery.</summary>
public sealed class ListOrdersByCustomerQueryHandler : IQueryHandler<ListOrdersByCustomerQuery, Result<IReadOnlyList<Order>>>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IOrderRepository _orderRepository;

    public ListOrdersByCustomerQueryHandler(ICustomerRepository customerRepository, IOrderRepository orderRepository)
    {
        _customerRepository = customerRepository;
        _orderRepository = orderRepository;
    }

    public async ValueTask<Result<IReadOnlyList<Order>>> Handle(ListOrdersByCustomerQuery query, CancellationToken cancellationToken)
    {
        var customerMaybe = await _customerRepository.FindByIdAsync(query.CustomerId, cancellationToken);
        if (customerMaybe.HasNoValue)
            return Result.Fail<IReadOnlyList<Order>>(
                new Error.NotFound(ResourceRef.For<Customer>(query.CustomerId)) { Detail = $"Customer {query.CustomerId} not found." });

        var orders = await _orderRepository.QueryAsync(
            new CustomerOrdersSpecification(query.CustomerId), cancellationToken);
        return Result.Ok(orders);
    }
}

/// <summary>Specification to filter orders by customer ID.</summary>
internal sealed class CustomerOrdersSpecification : Specification<Order>
{
    private readonly CustomerId _customerId;

    public CustomerOrdersSpecification(CustomerId customerId) =>
        _customerId = customerId;

    public override System.Linq.Expressions.Expression<Func<Order, bool>> ToExpression() =>
        order => order.CustomerId == _customerId;
}
